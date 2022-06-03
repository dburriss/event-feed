# Event Feed

[![Tests](https://github.com/dburriss/event-feed/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/dburriss/event-feed/actions/workflows/build-and-test.yml)
![Nuget](https://img.shields.io/nuget/v/EventFeed)

EventFeed is an opinionated way of eventing in your application. All events are stored in a log within the same transaction that the mutation to application state occurs. 

## Why would I use this?

If you are already using a T-SQL database and a .NET Core web API, you can get messaging without any additional infrastructure. The messages are durable, and "sending" them does not suffer from the [classic problem of loosing messages after saving state](https://devonburriss.me/reliability-with-intents/).

## Components

### Event persistance

![Nuget](https://img.shields.io/nuget/v/EventFeed.Producer.MSSQL)

As an example, `EventFeed.Producer.MSSQL` provides functionality for storing an **event** within the same transaction as an application mutation. 

### API middleware

![Nuget](https://img.shields.io/nuget/v/EventFeed.AspNetCore)

Provides an endpoint where events can be fetched as a page of events. It also ensures that complete pages are cached by the server or another configured caching mechanism. Lastly, it records metadata about the usage.

### Event Consumer

Provides an easy client for code to use to consume events from an event endpoint.

## Further reading

See the [design](/design) section.