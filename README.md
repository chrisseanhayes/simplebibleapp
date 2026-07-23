# Simple Bible App

Simple Bible App is a comprehensive, distributed web application for reading, studying, and interacting with the Bible. Built on modern .NET technologies and Microsoft Orleans, it is designed for scalability and performance.

## 🏗 Architecture & Technologies

The application follows a distributed architecture, separating the frontend web tier from the backend state management.

- **Frontend:** ASP.NET Core MVC
- **Backend Actor Framework:** [Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/) for scalable, distributed state management
- **Database (Auth & Lingustic Cache):** SQLite
- **Blob Storage (User Notes):** Azure Blob Storage (using [Azurite](https://github.com/Azure/Azurite) for local development)
- **Caching & Session:** Redis
- **Real-time Communication:** SignalR (for real-time linguistic/synonym engine processing)
- **Reverse Proxy:** Nginx
- **Containerization:** Docker & Docker Compose

## 📁 Project Structure

- `src/simplebibleapp/` - The main ASP.NET Core MVC frontend application.
- `src/simplebibleapp.Silo/` - The Microsoft Orleans Silo backend, managing grain states and distributed logic.
- `src/simplebibleapp.Orleans.Grains/` - Orleans Grain implementations.
- `src/simplebibleapp.Orleans.Interfaces/` - Interfaces defining the Orleans Grains contracts.
- `src/simplebibleapp.xmlbible/` & related - Core libraries for parsing, querying, and managing XML-based Bible data and dictionaries.
- `src/simplebibleapp.xmlbible.tests/` - Unit and integration tests.

## 🚀 Getting Started (Local Development)

The easiest way to run the application locally is using Docker Compose, which spins up all necessary services including the Web App, Orleans Silo, Redis, and Azurite.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
- .NET 8.0 SDK (if running or building outside of Docker).

### Running the App

1. Clone the repository and navigate to the root directory.
2. Ensure you have the proper development certificates or allow the default generated ones.
3. Start the application using Docker Compose from the `src` directory:

```bash
cd src
docker-compose up --build
```

This will launch:
- `sbaweb` (Frontend MVC app) on `http://localhost:5001` (or via Nginx on `80`/`443`)
- `sbasilo` (Orleans Backend)
- `azurite` (Azure Storage Emulator)
- `sbaredis` (Redis cache)
- `nginx` & `certbot`

### Accessing the App
Once the containers are running, you can access the app in your browser at:
`http://localhost` or `http://localhost:5001`

## 🔑 Key Features

- **Bible Reading & Navigation:** Browse books, chapters, and verses seamlessly.
- **Search:** Full-text search across the Bible text.
- **Linguistic Engine:** Advanced linguistic tools and synonym processing using a cached engine.
- **User Notes:** Users can attach and filter custom notes to specific verses (stored securely in Azure Blob Storage).
- **Authentication:** Built-in ASP.NET Core Identity using a SQLite backing store.

## 🛠 Testing

A test suite is included in `src/simplebibleapp.xmlbible.tests`. You can run tests locally using the .NET CLI:

```bash
cd src/simplebibleapp.xmlbible.tests
dotnet test
```

## 📸 Screenshots & Playwright Automation

*(Optional)* You can generate automated screenshots of the application for this README or documentation using Playwright. See the `scripts/screenshots` directory for the automation script.
