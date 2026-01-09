# Паттерн: Strategy (Стратегия)

Создайте интерфейс `IPaymentStrategy` с одним методом расчета или обработки платежа (например, `decimal Calculate(decimal amount)` или `void Pay(decimal amount)`).
Реализуйте минимум две стратегии оплаты с разной логикой (например, `CardPaymentStrategy` и `CashPaymentStrategy`).
Создайте класс `PaymentProcessor` (контекст), который принимает `IPaymentStrategy` через конструктор и использует ее внутри метода `Process`.
Покажите, что стратегию можно заменить без изменения логики контекста.
