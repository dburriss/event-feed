# Producer design

The producer is responsible for:

- serving the HTTP requests for pages of events
- output/response caching of completed pages
- collecting of meta data about usage (see persistence design for details)
- only serve the event feed to authorized consumers

## How

[See caching discussion here](https://github.com/dburriss/event-feed/issues/2).
 
 ```mermaid
sequenceDiagram

    participant DB as Event Data Store
    participant S as Producer Cache
    participant P as Producer
    participant C as Consumer

    C ->>+ P: Fetch meta data
    P -> DB: Retrieve meta data from DB
    P -->>- C: Return meta data
    
    C ->>+ P: Fetch a page
    P ->> S: Try fetch page
    alt Page is in cache
        S -->> P: Returns page of events
        else Page is not in cache
        S -->> P: Cache miss
        P ->> DB: Fetch page from DB
        DB -->> P: Return page 
        alt Is a complete page? ie. 100 events
            P ->> S: Place page in cache (long TTL)
        else Is last page and not complete
            P ->> S: Place page in cache (short TTL)
        end
    end
    P -->>- C: Return page
    P ->> DB: Update meta data (background)
```

