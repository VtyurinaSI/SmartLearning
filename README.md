# SmartLearning
SmartLearning — распределенная платформа для обучения паттернам программирования с автоматизированной проверкой решений.
Система принимает пользовательский исходный код, компилирует его, выполняет рефлексионные проверки, инициирует LLM-ревью и фиксирует ход проверки в подсистеме прогресса.
Платформа рассчитана на сценарии обучения и самопроверки, где важны воспроизводимость проверок, трассируемость результатов и единый контур мониторинга.

## Назначение и решаемая задача
Платформа обеспечивает полный цикл проверки задания по паттернам:
- прием и хранение отправленного кода;
- воспроизводимую компиляцию и сборку артефактов;
- проверку структуры/сигнатур через рефлексию;
- оценку качества решения при помощи LLM;
- хранение и отображение статусов выполнения.
Таким образом, SmartLearning решает задачу централизованной и прозрачной валидации учебных решений в микросервисной архитектуре.

## Схема микросервисов и потоков
```
Клиент
  |
  v
Gateway (API + UI) ---> AuthService
  |
  v
Orchestrator.Application
  |----> CompilerService --------\
  |----> ReflectionService -------+--> ProgressService
  |----> LlmService --------------/
  |				└> Ollama
  v
ObjectStorageService <--> MinIO

PatternService ---> Postgres (каталог и материалы)
AuthService, UserService, ProgressService ---> Postgres
Orchestrator/Compiler/Reflection/Llm ---> RabbitMQ
```

Ключевые обязанности сервисов:
- Gateway: внешний HTTP-вход, UI, проксирование и обогащение заголовков.
- Orchestrator.Application: оркестрация проверочного процесса, координация стадий.
- CompilerService: компиляция исходного кода и формирование артефактов.
- ReflectionService: выполнение структурных проверок с использованием рефлексии.
- LlmService: генерация ревью по материалам и результатам проверки.
- ProgressService: агрегация и выдача статусов проверки.
- ObjectStorageService: унифицированный доступ к объектному хранилищу.
- PatternService: каталог заданий, теории и материалов.
- AuthService/UserService: учетные данные, профили и роли.

## Как запустить 
Единый вариант запуска через Docker:
```
docker-compose up --build
```

## Демо-сценарий
1) Пользователь загружает исходный код через Gateway.
2) Orchestrator инициирует проверку и публикует задания в RabbitMQ.
3) CompilerService компилирует решение и сохраняет артефакты в MinIO через ObjectStorageService.
4) ReflectionService скачивает сборку и выполняет рефлексионные проверки структуры и сигнатур.
5) LlmService формирует текстовое ревью на основе материалов и результата проверки.
6) ProgressService агрегирует статусы и предоставляет итоговый прогресс пользователю.

## Где что смотреть
Swagger-интерфейсы:
- Gateway: http://localhost:5000/swagger
- Orchestrator: http://localhost:6000/swagger
- AuthService: http://localhost:6011/swagger
- UserService: http://localhost:6001/swagger
- ProgressService: http://localhost:6010/swagger
- PatternService: http://localhost:6012/swagger
- ObjectStorageService: http://localhost:6005/swagger
- CompilerService: http://localhost:6002/swagger
- ReflectionService: http://localhost:6004/swagger
- LlmService: http://localhost:6003/swagger

Инфраструктура:
- MinIO (UI): http://localhost:9001
- RabbitMQ (UI): http://localhost:15672 (логин/пароль по умолчанию)
- Postgres: localhost:5432 (параметры в `docker-compose.yml`)

Логи:
- `docker compose logs -f <service>` для просмотра журналов конкретного сервиса.
