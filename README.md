# event-feed

EventFeed is an opinionated way of eventing in your application. All events are stored in a log within the same transaction that the mutation to application state occurs. 

## Why would I use this?

Firstly, the persistence mechanism can be used to easily setup an Outbox pattern for resilient collaboration with other infrastructure like a message queue or another server.

## Components

### Event persistance

As an example, `EventFeed.Producer.MSSQL` provides functionality for storing an **event** within the same transaction as an application mutation. 

### API middleware

Provides an endpoint where events can be fetched as a page of events. It also ensures that complete pages are cached by the server or another configured caching mechanism. Lastly, it records metadata about the usage.

### Event Consumer

Provides an easy client for code to use to consume events from an event endpoint.

## Further reading

See the [design](/design) section.