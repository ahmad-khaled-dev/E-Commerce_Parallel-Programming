# E-Commerce — High-Performance Backend Engine

مشروع مادة البرمجة المتوازية · فصل 2026

---

## المتطلب 3 — المعالجة غير المتزامنة (Asynchronous Queues)

### الفكرة

عند إتمام طلب شراء، يحتاج النظام إلى تنفيذ مهام جانبية ثقيلة مثل:
- توليد الفاتورة (PDF generation)
- إرسال إشعار تأكيد الطلب (Email / SMS)

هذه المهام **لا يحتاج المستخدم لانتظار نتيجتها**، لكن إذا نُفِّذت بشكل متزامن داخل الـ request فإنها تُبطئ الاستجابة دون مبرر.

---

### المشكلة — Synchronous Side-Tasks

```
POST /api/asyncqueuedemo/without-queue
Body: 1
```

**ما يحدث داخل الكود:**

```csharp
// OrderController يستدعي هذا الترتيب بشكل متسلسل:

await Task.Delay(50);      // المنطق الأساسي (سريع)
await SimulateInvoiceGenerationAsync();   // ينتظر ~1000ms
await SimulateEmailNotificationAsync();   // ينتظر ~1000ms

// المستخدم انتظر ~2050ms بينما كان يكفيه 50ms فقط
```

**نتيجة الاستجابة:**

```json
{
  "case": "PROBLEM — Synchronous side-tasks",
  "orderId": 1,
  "elapsedMs": 2053,
  "userWaitedFor": "Core order logic + invoice generation + email notification",
  "verdict": "User blocked for 2053 ms. Only ~50 ms were necessary; the rest was avoidable waiting."
}
```

**المشكلة المثبتة:** المستخدم يجلس ينتظر ~2 ثانية لمهام لا علاقة لها بإتمام طلبه.

---

### الحل — Async Queue (Channel\<T\> + BackgroundService)

```
POST /api/asyncqueuedemo/with-queue
Body: 1
```

**البنية المستخدمة:**

```
HTTP Request Thread                    Background Thread (NotificationWorker)
─────────────────────────              ───────────────────────────────────────
[1] Core logic (~50ms)                 ← دائماً يراقب Channel
[2] Enqueue(InvoiceGeneration)   ──►  [3] DequeueAsync() → ProcessAsync() ~2s
[3] Enqueue(EmailConfirmation)   ──►  [4] DequeueAsync() → ProcessAsync() ~2s
[4] return 200 OK  (~52ms)            (يعمل بعد انتهاء الـ response بثوانٍ)
```

**المكونات المُنفَّذة:**

| الملف | الدور |
|---|---|
| `Application/interfaces/INotificationQueue.cs` | Contract بين الـ Controller والـ Queue |
| `Infrastructure/Services/NotificationQueue.cs` | تنفيذ الـ Queue باستخدام `Channel<NotificationMessage>` |
| `Infrastructure/BackgroundJobs/NotificationWorker.cs` | `BackgroundService` يستهلك الرسائل من الـ Channel |

**لماذا `Channel<T>` وليس `Thread` عادي أو `Task.Run`؟**

- `Channel<T>` هو **pipe آمن للخيوط (thread-safe)** مبني في .NET
- يدعم **Back-pressure**: إذا امتلأ الـ buffer يُوقف المُنتِج بدلاً من رمي خطأ أو تجاهل الرسائل
- `BoundedChannel(capacity: 1000)` يمنع نمو الذاكرة بشكل غير محدود
- `NotificationWorker` يعيش طوال عمر التطبيق وهو `Singleton`

**نتيجة الاستجابة:**

```json
{
  "case": "SOLUTION — Async Queue (Channel<T> + BackgroundService)",
  "orderId": 1,
  "elapsedMs": 52,
  "userWaitedFor": "Core order logic only",
  "verdict": "User received response in 52 ms. Invoice + email are being processed by NotificationWorker in the background. Check application logs to see when they complete (~2 s later)."
}
```

**في الـ logs بعد ~2 ثانية:**

```
[NotificationWorker] Processed | Type=InvoiceGeneration | OrderId=1 | UserId=1 | Amount=$149.99
[NotificationWorker] Processed | Type=EmailConfirmation | OrderId=1 | UserId=1 | Amount=$149.99
```

---

### مقارنة قبل وبعد — المتطلب 3

| المعيار | بدون Queue (المشكلة) | مع Queue (الحل) |
|---|---|---|
| زمن استجابة HTTP | ~2053 ms | ~52 ms |
| هل المستخدم ينتظر الفاتورة؟ | نعم | لا |
| هل يمكن معالجة 100 طلب متزامن؟ | كل طلب يحجز Thread لمدة 2s | نعم، الـ Threads تتحرر فوراً |
| خطر تراكم الطلبات؟ | مرتفع (Thread starvation) | منخفض (Channel يخزّن مؤقتاً) |
| نقطة المزامنة | `await` متسلسل يحجب الـ Thread | `Channel.Writer.TryWrite` غير حاجب |

---
---

## المتطلب 4 — معالجة البيانات على دفعات (Batch Processing)

### الفكرة

في نهاية كل يوم يحتاج النظام لجرد كل المبيعات اليومية وحساب:
- إجمالي الإيرادات
- عدد الطلبات المعالجة

إذا تراكمت آلاف الطلبات يومياً، تحميلها **مرة واحدة** في الذاكرة يُسبب مشاكل خطيرة.

---

### المشكلة — No Batching (All Records At Once)

```
GET /api/batchprocessingdemo/without-batching?date=2026-05-08
```

**ما يحدث داخل الكود:**

```csharp
// يُحمِّل كل سجلات اليوم في RAM دفعة واحدة
var allOrders = await _context.Orders
    .Where(o => o.CreatedAt.Date == date.Date)
    .ToListAsync();  // ← إذا كان هناك مليون طلب → مليون سجل في RAM

decimal totalRevenue = allOrders.Sum(o => o.TotalAmount);
```

**المشاكل:**

1. **Memory Spike**: إذا كان هناك مليون طلب → يُحمِّل ملايين الكائنات في الـ RAM دفعةً واحدة
2. **Long DB Connection**: الاتصال بقاعدة البيانات يظل مفتوحاً حتى تنتهي القراءة الكاملة
3. **No Thread Yield**: لا توجد فرصة لتنفيذ مهام أخرى على الـ thread pool أثناء المعالجة
4. **Single Large Query**: استعلام واحد ضخم قد يُشغِّل الـ DB لوقت طويل

**نتيجة الاستجابة:**

```json
{
  "approach": "PROBLEM — No Batching (all records loaded at once)",
  "reportDate": "2026-05-08",
  "totalOrdersProcessed": 250,
  "totalRevenue": 18750.00,
  "chunkSize": 250,
  "totalChunks": 1,
  "elapsedMs": 45,
  "verdict": "All 250 records pulled into RAM in one query. Risk: OutOfMemoryException on large tables."
}
```

---

### الحل — Chunked Processing (Skip/Take + Task.Yield)

```
GET /api/batchprocessingdemo/with-batching?date=2026-05-08&chunkSize=10
```

**ما يحدث داخل الكود:**

```csharp
int skip = 0;
while (true)
{
    // تحميل دفعة صغيرة فقط (10 سجلات)
    var chunk = await _context.Orders
        .Where(o => o.CreatedAt.Date == date.Date)
        .OrderBy(o => o.Id)   // ترتيب ثابت ضروري لصحة Skip/Take
        .Skip(skip)
        .Take(chunkSize)
        .ToListAsync();

    if (chunk.Count == 0) break;   // لا توجد سجلات أخرى

    totalRevenue += chunk.Sum(o => o.TotalAmount);
    totalProcessed += chunk.Count;
    skip += chunkSize;

    // تحرير الـ Thread بين الدفعات → تبقى المهام الأخرى مجدولة
    await Task.Yield();
}
```

**لماذا `Task.Yield()` مهم؟**

بدون `Task.Yield()` يمكن لـ Job ثقيل أن يستحوذ على Thread Pool Thread لفترة طويلة ويمنع طلبات HTTP الأخرى من المعالجة.  
`Task.Yield()` يُعيد جدولة الـ continuation ويُحرر الـ Thread مؤقتاً بعد كل دفعة.

**المكونات المُنفَّذة:**

| الملف | الدور |
|---|---|
| `Application/interfaces/IBatchSalesProcessor.cs` | Contract يعرّف الحالتين |
| `Infrastructure/Services/BatchSalesProcessor.cs` | تنفيذ المعالجة (بدون دفعات / بدفعات) |
| `Infrastructure/BackgroundJobs/DailySalesBatchJob.cs` | `BackgroundService` يشغّل الحل تلقائياً كل 60 ثانية |

**نتيجة الاستجابة:**

```json
{
  "approach": "SOLUTION — Chunked Processing (chunkSize = 10)",
  "reportDate": "2026-05-08",
  "totalOrdersProcessed": 250,
  "totalRevenue": 18750.00,
  "chunkSize": 10,
  "totalChunks": 25,
  "elapsedMs": 120,
  "verdict": "Processed 250 orders in 25 chunks of ≤10. Memory bounded to ~10 rows at a time; thread yielded between chunks."
}
```

**في الـ logs تجد سير المعالجة خطوة بخطوة:**

```
[BatchSalesProcessor] Chunk 1: 10 orders processed (cumulative: 10).
[BatchSalesProcessor] Chunk 2: 10 orders processed (cumulative: 20).
...
[BatchSalesProcessor] Chunk 25: 10 orders processed (cumulative: 250).

[DailySalesBatchJob] Completed | Date=05/08/2026 | Orders=250 | Revenue=$18,750.00 | Chunks=25 | Elapsed=120ms
```

---

### DailySalesBatchJob — الجدولة التلقائية

الـ Job مسجَّل كـ `IHostedService` ويعمل فور تشغيل التطبيق:

```csharp
// يعمل كل 60 ثانية (للتجربة) — في الإنتاج يُغيَّر إلى TimeSpan.FromHours(24)
private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
```

**لماذا `IServiceScopeFactory` وليس حقن `IBatchSalesProcessor` مباشرة؟**

`BackgroundService` هو `Singleton` بينما `IBatchSalesProcessor` يعتمد على `AppDbContext` الذي هو `Scoped`.  
حقن `Scoped` داخل `Singleton` مباشرةً يُسبب **Captive Dependency** → خطأ في الـ runtime.  
الحل: نُنشئ `Scope` جديد لكل تشغيل:

```csharp
await using var scope = _scopeFactory.CreateAsyncScope();
var processor = scope.ServiceProvider.GetRequiredService<IBatchSalesProcessor>();
```

---

### مقارنة قبل وبعد — المتطلب 4

| المعيار | بدون Batching (المشكلة) | مع Batching (الحل) |
|---|---|---|
| الذاكرة المستخدمة (1M سجل) | ~1 GB (كل السجلات في RAM) | ~ثابتة بحجم الـ chunk فقط |
| عدد الاستعلامات لـ DB | 1 استعلام ضخم | N استعلام صغير |
| هل يُحرَّر الـ Thread؟ | لا | نعم (Task.Yield بين الدفعات) |
| خطر OutOfMemoryException | مرتفع جداً | منعدم |
| تأثير على الطلبات المتزامنة | يحجب الـ thread pool | لا تأثير (Thread يُحرَّر دورياً) |

---

## كيفية الفحص خطوة بخطوة

### 1. تشغيل التطبيق

```bash
cd E-Commerce_Parallel-Programming
dotnet run --project E-Commerce.Api
```

ثم افتح Swagger UI على:
```
http://localhost:5000/swagger
```

---

### 2. فحص المتطلب 3

**الخطوة 1 — الحالة المشكلة:**

```http
POST /api/asyncqueuedemo/without-queue
Content-Type: application/json

1
```

لاحظ في الـ response أن `elapsedMs` ≈ **2000ms**.

**الخطوة 2 — الحالة الحل:**

```http
POST /api/asyncqueuedemo/with-queue
Content-Type: application/json

1
```

لاحظ أن `elapsedMs` ≈ **50ms**، ثم راقب الـ **console/logs** — ستجد بعد ~2 ثانية:

```
[NotificationWorker] Processed | Type=InvoiceGeneration ...
[NotificationWorker] Processed | Type=EmailConfirmation ...
```

**نقطة المقارنة:** نفس العملية، الفرق فقط في **متى** تُنفَّذ المهام الجانبية.

---

### 3. فحص المتطلب 4

**الخطوة 1 — الحالة المشكلة:**

```http
GET /api/batchprocessingdemo/without-batching?date=2026-05-08
```

لاحظ في الـ response:
- `totalChunks: 1` ← كل شيء في استعلام واحد
- `chunkSize` = عدد السجلات الكلي

**الخطوة 2 — الحالة الحل:**

```http
GET /api/batchprocessingdemo/with-batching?date=2026-05-08&chunkSize=10
```

لاحظ في الـ response:
- `totalChunks` = عدد الدفعات
- `chunkSize: 10` ← الذاكرة محدودة بـ 10 سجلات في أي لحظة
- في الـ logs ستجد سطراً لكل دفعة

**الخطوة 3 — فحص الـ Job التلقائي:**

عند تشغيل التطبيق ستجد في الـ console كل 60 ثانية:

```
[DailySalesBatchJob] Completed | Date=... | Orders=... | Revenue=... | Chunks=... | Elapsed=...ms
```

---

## هيكل الملفات المضافة

```
E-Commerce.Application/
├── DTOs/
│   ├── Notification/
│   │   └── NotificationMessage.cs        ← رسالة الـ Queue
│   └── Batch/
│       └── BatchSalesReportDto.cs        ← نتيجة تقرير الدفعات
└── interfaces/
    ├── INotificationQueue.cs             ← Contract للـ Queue
    └── IBatchSalesProcessor.cs           ← Contract للمعالجة بالدفعات

E-Commerce.Infrastructure/
├── Services/
│   ├── NotificationQueue.cs              ← Channel<T> implementation
│   └── BatchSalesProcessor.cs           ← منطق المعالجة (حالتان)
└── BackgroundJobs/
    ├── NotificationWorker.cs             ← مستهلك الـ Queue
    └── DailySalesBatchJob.cs            ← Job الجرد اليومي التلقائي

E-Commerce.Api/
└── Controllers/
    ├── AsyncQueueDemoController.cs       ← Endpoint المتطلب 3 (حالتان)
    └── BatchProcessingDemoController.cs  ← Endpoint المتطلب 4 (حالتان)
```
