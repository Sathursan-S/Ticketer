# Ticketer Application - Docker Instructions

This document provides instructions on how to build and run the entire Ticketer application using Docker.

## Services

The Ticketer application consists of the following microservices:

1. Authentication Service (Port: 4040)
2. Events Service (Port: 8081)
3. Notification Service (Port: 8082)
4. Booking Service (Port: 8080)
5. Ticket Service (Port: 8083)

Each service has its own database and can be run independently or as part of the complete system.

## Building and running your application

When you're ready, start your application by running:
`docker compose up --build`.

This will build and start all services and their databases with the following access points:

- Authentication Service: http://localhost:4040
- Events Service: http://localhost:8081
- Notification Service: http://localhost:8082
- Booking Service: http://localhost:8080
- Ticket Service: http://localhost:8083

To run in detached mode:

```sh
docker compose up -d
```

To stop all services:

```sh
docker compose down
```

## Running Individual Services

Each service has its own Docker Compose file in its respective directory. You can navigate to a service directory and run:

```sh
docker compose up
```

## Database Ports

- Authentication DB: 5432
- Events DB: 5433
- Notification DB: 5434

### Deploying your application to the cloud

First, build your image, e.g.: `docker build -t myapp .`.
If your cloud uses a different CPU architecture than your development
machine (e.g., you are on a Mac M1 and your cloud provider is amd64),
you'll want to build the image for that platform, e.g.:
`docker build --platform=linux/amd64 -t myapp .`.

Then, push it to your registry, e.g. `docker push myregistry.com/myapp`.

Consult Docker's [getting started](https://docs.docker.com/go/get-started-sharing/)
docs for more detail on building and pushing.