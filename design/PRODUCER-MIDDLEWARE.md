# Producer design

The producer is responsible for:

- serving the HTTP requests for pages of events
- output caching of completed pages
- collecting of meta data about usage (see persistence design for details)
- only serve the event feed to authorized consumers