# HeartMeet 💕

Веб-приложение для знакомств на Blazor Server + .NET 8 + PostgreSQL.

## Быстрый старт

### Локально

```bash
# Создать БД
psql -U postgres -c "CREATE USER heartmeet WITH PASSWORD 'heartmeet_pass' SUPERUSER;"
psql -U postgres -c "CREATE DATABASE heartmeet_db OWNER heartmeet;"

cd src/HeartMeet.Web
dotnet run
```

Открыть: **http://localhost:5000**

### Docker

```bash
docker-compose up -d --build
```

Открыть: **http://localhost:5001**

## Тест-аккаунты

| Email | Пароль | Роль |
|---|---|---|
| admin@heartmeet.ru | Admin123! | Администратор |
| anna@test.ru | Test123! | Анна, 25 лет |
| alex@test.ru | Test123! | Алексей, 28 лет |
| kate@test.ru | Test123! | Катя, 23 года |
