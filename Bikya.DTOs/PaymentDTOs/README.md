# Payment DTOs Documentation

## Overview

تم تقسيم DTOs الخاصة بالدفع إلى classes منفصلة لتحسين التنظيم والمرونة.

## DTOs Available

### 1. PaymentRequestDto
**الغرض**: طلب إنشاء دفعة جديدة

```csharp
public class PaymentRequestDto
{
    public decimal Amount { get; set; }        // المبلغ المطلوب دفعه
    public int OrderId { get; set; }          // رقم الطلب
    public int UserId { get; set; }           // رقم المستخدم
    public string? Description { get; set; }  // وصف الدفعة (اختياري)
}
```

### 2. PaymentResponseDto
**الغرض**: استجابة إنشاء الدفعة مع معلومات Stripe

```csharp
public class PaymentResponseDto
{
    public int PaymentId { get; set; }           // رقم الدفعة
    public decimal Amount { get; set; }          // المبلغ
    public int OrderId { get; set; }             // رقم الطلب
    public string Status { get; set; }           // حالة الدفعة
    public string StripeUrl { get; set; }        // رابط Stripe للدفع
    public string StripeSessionId { get; set; }  // معرف جلسة Stripe
    public DateTime CreatedAt { get; set; }      // تاريخ الإنشاء
    public string Message { get; set; }          // رسالة توضيحية
}
```

### 3. PaymentStatusDto
**الغرض**: حالة الدفعة مع تفاصيل إضافية

```csharp
public class PaymentStatusDto
{
    public int PaymentId { get; set; }           // رقم الدفعة
    public decimal Amount { get; set; }          // المبلغ
    public int OrderId { get; set; }             // رقم الطلب
    public string Status { get; set; }           // حالة الدفعة
    public string StripeSessionId { get; set; }  // معرف جلسة Stripe
    public DateTime CreatedAt { get; set; }      // تاريخ الإنشاء
    public DateTime? CompletedAt { get; set; }   // تاريخ الإكمال (إذا تم الدفع)
    public string Message { get; set; }          // رسالة توضيحية
}
```

### 4. PaymentSummaryDto
**الغرض**: ملخص مختصر للدفعة

```csharp
public class PaymentSummaryDto
{
    public int PaymentId { get; set; }           // رقم الدفعة
    public decimal Amount { get; set; }          // المبلغ
    public string Status { get; set; }           // حالة الدفعة
    public DateTime CreatedAt { get; set; }      // تاريخ الإنشاء
    public string Description { get; set; }      // وصف الدفعة
}
```

### 5. PaymentErrorDto
**الغرض**: تفاصيل الأخطاء في الدفع

```csharp
public class PaymentErrorDto
{
    public string ErrorCode { get; set; }                    // رمز الخطأ
    public string ErrorMessage { get; set; }                 // رسالة الخطأ
    public List<string> ValidationErrors { get; set; }       // أخطاء التحقق
    public string? SuggestedAction { get; set; }             // الإجراء المقترح
}
```

## API Endpoints

### إنشاء دفعة موحدة
```
POST /api/Wallet/Payment/unified
```

**Request Body:**
```json
{
  "amount": 150.00,
  "orderId": 123,
  "userId": 456,
  "description": "دفع للطلب رقم 123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "paymentId": 789,
    "amount": 150.00,
    "orderId": 123,
    "status": "Pending",
    "stripeUrl": "https://checkout.stripe.com/pay/cs_test_...",
    "stripeSessionId": "cs_test_...",
    "createdAt": "2024-01-15T10:30:00Z",
    "message": "تم إنشاء جلسة الدفع بنجاح"
  }
}
```

### الحصول على حالة الدفعة
```
GET /api/Wallet/Payment/unified/status/{paymentId}
```

### الحصول على ملخص الدفعة
```
GET /api/Wallet/Payment/summary/{paymentId}
```

## Benefits of Separated DTOs

✅ **تنظيم أفضل**: كل DTO له غرض محدد
✅ **مرونة أكبر**: يمكن استخدام كل DTO بشكل منفصل
✅ **صيانة أسهل**: تعديل DTO واحد لا يؤثر على الآخرين
✅ **أداء أفضل**: إرسال البيانات المطلوبة فقط
✅ **توثيق أوضح**: كل DTO يوضح الغرض منه

## Usage Examples

### Frontend Integration
```typescript
// إنشاء دفعة
const createPayment = async (order: Order) => {
  const request: PaymentRequestDto = {
    amount: order.totalAmount,
    orderId: order.id,
    userId: currentUser.id,
    description: `دفع للطلب ${order.id}`
  };
  
  const response = await api.post('/api/Wallet/Payment/unified', request);
  return response.data;
};

// التحقق من حالة الدفعة
const checkPaymentStatus = async (paymentId: number) => {
  const response = await api.get(`/api/Wallet/Payment/unified/status/${paymentId}`);
  return response.data;
};
```

### Error Handling
```typescript
try {
  const payment = await createPayment(order);
  if (payment.success) {
    window.location.href = payment.data.stripeUrl;
  }
} catch (error) {
  const errorData: PaymentErrorDto = error.response.data;
  console.error('Payment Error:', errorData.errorMessage);
  showError(errorData.validationErrors);
}
``` 