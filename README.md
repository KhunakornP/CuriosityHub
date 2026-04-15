# CuriosityHub
A web application for passionate researchers to share their research to a casual audience.

## Description
A web application for passionate researchers to share their research to a casual audience.

## System architecture overview
Curiosity hub follows a microservice design.
Each distributed service has it's own responsibilities outlined below
| Service | Responsibility |
| :--- | :--- |
| **Admin Gateway** | Proxy for other microservices for the admin frontend |
| **Gateway** | Proxy for other microservices for the video streaming frontend |
| **VideoStreaming** | Serve content for the frontend, provide caching. |
| **VideoUpload** | Mediator for video uploads. Ensure data consistency for video uploads |
| **IdentityService** | Handles authentication and user profiles. JWT issuing and verification |
| **VideoViews** | Tracks views for each video |
| **VideoMetadata** | Stores details for each video. |
| **VideoStorage** | Provides storage for video files |
| **Streaming (frontend)** | Provides a web interface for users (viewers, researchers) |
| **Admin (frontend)** | Provides a web interface for admins |

## User roles
The application has the following user roles

| User Role | Description | Permissions |
| :------------- | :--- | :--- |
| **Admin**      | An admin who manages the system, allows manual CRUD operations for users, videos, and comments. | <ul><li>Researcher permissions</li><li>Delete Comments</li><li>Delete Videos</li><li>Delete Users</li></ul> |
| **Researcher** | A user who uploads and edits research videos, allows for CRUD operations for videos in addition to regular viewer permissions. | <ul><li>Viewer permissions</li><li>Create Videos</li><li>Update Videos</li></ul>|
| **Viewer**     | An authenticated user who can leave comments. | <ul><li>Create comments</li><li>Update comments</li><li>Update Profile</li><li>View comments</li><li>View videos</li><li>View profiles</li></ul>|

## Technology stack
Curiosity hub uses the following frameworks and libraries

Languages:
- C#
- Python

Frameworks:
- ASP .Net
- Vue
- Axios
- RabbitMQTT

## Installation & setup instructions
### Pre-requisites
- Docker (or a suitable alternative like Podman)
- .NET SDK
- Node.js (v24 or above)

### Installation setup
1. Clone the repository to your local machine:
   ```bash
   git clone <repository-url>
   cd CuriosityHub
   ```

2. Install dependencies for the streaming frontend:
   ```bash
   cd src/frontend/streaming
   npm install
   ```

3. Install dependencies for the admin frontend:
   ```bash
   cd ../admin
   npm install
   ```

4. Configure environment variables:
   Copy the provided `sample.env` file to create a `.env` file in the `src` directory.
   ```bash
   cd ../..
   cp src/sample.env src/.env
   ```

5. (optional) configure environment variables
   The `.env` file contains the following configurations that you can adjust:
   | Variable | Description |
   | :--- | :--- |
   | `ASPNETCORE_ENVIRONMENT` | Sets the application environment (e.g., `Development`, `Production`). |
   | `MARIADB_ROOT_PASSWORD` | Root password for the MariaDB instances. |
   | `COMMENT_DB_NAME` | Database name for the Comment Service. |
   | `COMMENT_DB_CONNECTION_STRING` | Connection string for the Comment Service to connect to its database. |
   | `IDENTITY_DB_NAME` | Database name for the Identity Service. |
   | `IDENTITY_DB_CONNECTION_STRING` | Connection string for the Identity Service to connect to its database. |
   | `MONGO_STREAMING_CONNECTION_STRING` | MongoDB connection URL for the Video Streaming Service. |
   | `MONGO_VIEWS_CONNECTION_STRING` | MongoDB connection URL for the Video Views track Service. |
   | `MONGO_METADATA_CONNECTION_STRING` | MongoDB connection URL for the Video Metadata Service. |
   | `RABBITMQ_USER` | Default username for the RabbitMQ message broker. |
   | `RABBITMQ_PASS` | Default password for the RabbitMQ message broker. |
   | `RABBITMQ_CONNECTION_STRING` | Connection string / hostname for interacting with the RabbitMQ instance. |
   | `JWT_SECRET` | Secret key used for signing and verifying JWTs in the Identity Service. |
   | `JWT_ISSUER` | Issuer URL for the generated JSON Web Tokens. |


## Running the application

> **⚠️ Warning:** The `npm run dev` commands used below start a local development server. A real production environment needs to not use `npm run dev`. Instead, you should build the frontend applications (e.g., `npm run build`) and serve the resulting static assets with a production-ready web server like Nginx.

1. Start the microservices and required infrastructure using Docker Compose:
   ```bash
   cd src
   docker compose up -d --build
   ```

2. Start the streaming frontend application:
   ```bash
   cd src/frontend/streaming
   npm run dev
   ```

3. Start the admin frontend application:
   ```bash
   cd src/frontend/admin
   npm run dev
   ```

## Stopping the application
1. Stop the microservices
   ```bash
   docker compose down
   ```

## Screenshots

### Admin Dashboard
![Admin Comments Management](./screenshots/admin%20c.JPG)
*Admin Comments Management*

![Admin Users Management](./screenshots/admin%20u.JPG)
*Admin Users Management*

![Admin Videos Management](./screenshots/admin%20v.JPG)
*Admin Videos Management*

### Streaming Frontend
![Streaming Home Page](./screenshots/streaming%20home.JPG)
*Streaming Home Page*

![Sign In Page](./screenshots/sign%20in.JPG)
*Sign In Page*

![Upload Video Page](./screenshots/upload%20video.JPG)
*Upload Video Page*

![Video Streaming Player](./screenshots/video%20streaming.JPG)
*Video Streaming Player*

![Researcher Profile Overview](./screenshots/profile%20page%20r.JPG)
*Researcher Profile Overview*

![Researcher Profile Details](./screenshots/profile%20page%20r2.JPG)
*Researcher Profile Details*
