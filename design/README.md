# Design

This is the an outline of the design goals and different components that make up the event feed to achieve said goals.

## Design goals

On a high level, these libraries provide building blocks allowing a system to:

- Store events the represent facts about what has happened in the application
- Make those historical events available for consumption

### How:

- Create small building blocks that provide flexibility for developers to use as needed
- The design decisions should never lead to data loss if used as recommended
- Telemetry should be a first class citizen in the design
- Performance should scale from 100 records to millions of events
- Persistence should be able to collaborate in the transactions that save a state change from said event ie. atomic

## Components

- [Persistence](PRODUCER-PERSISTENCE.md)
- [Producer middleware](PRODUCER-MIDDLEWARE.md)
- [Consumer](CONSUMER.md)
