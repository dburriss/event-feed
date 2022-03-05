# Consumer design

The consumer library offers helpers for consuming events from a event producer.

Things to consider:

- resilient fetching of new events
- persisting the last seen page
- mapping events to new data/actions

This library should not try to do too much but focus on tools to make it easier to fetch events and action on them.