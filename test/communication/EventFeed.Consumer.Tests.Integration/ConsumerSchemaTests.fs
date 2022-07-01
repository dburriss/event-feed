namespace EventFeed.Consumer.Tests.Integration
module ConsumerSchemaTests =

    open System
    open Xunit
    open EventFeed.Consumer

    [<Fact>]
    let ``Consumer honours the meta page schema`` () =
        let json = """
        {
          "eventCount": 1,
          "eventsPerPage": 100,
          "pageCount": 2,
          "_links": {
            "self": {
              "href": "/api/event-feed",
              "templated": false,
              "rel": null
            },
            "head": {
              "href": "/api/event-feed/pages/1",
              "templated": false,
              "rel": null
            },
            "tail": {
              "href": "/api/event-feed/pages/2",
              "templated": false,
              "rel": null
            },
            "page": {
              "href": "/api/event-feed/pages/{pageNumber}",
              "templated": true,
              "rel": null
            }
          }
        }
        """
        let meta = Meta.Deserialize(json)

        Assert.Equal(1L, meta.eventCount)
        Assert.Equal(100, meta.eventsPerPage)
        Assert.Equal(2, meta.pageCount)
        Assert.Equal("/api/event-feed", meta._links.self.href)
        Assert.Equal("/api/event-feed/pages/1", meta._links.head.href)
        Assert.Equal("/api/event-feed/pages/2", meta._links.tail.href)
        Assert.Equal("/api/event-feed/pages/{pageNumber}", meta._links.page.href)
        Assert.False(meta._links.self.templated)
        Assert.False(meta._links.head.templated)
        Assert.False(meta._links.tail.templated)
        Assert.True(meta._links.page.templated)

    [<Fact>]
    let ``Consumer honours the page schema``() =
        let json = """
        {
          "pageNumber": 2,
          "events": [
            {
              "eventId": "0d65235c-74c3-4ea1-8b99-cf8918de50ba",
              "eventName": "test-event",
              "eventSchemaVersion": 1,
              "payload": "{ clicked: true }",
              "sequenceNumber": 1,
              "spanId": "21231367060a2828",
              "createdAt": "2022-07-01T09:48:58.0123019+00:00",
              "traceId": "4ea49da4daca55d29ff126d6da27416a"
            }
          ],
          "_links": {
            "meta": {
              "href": "/api/event-feed",
              "templated": false,
              "rel": null
            },
            "head": {
              "href": "/api/event-feed/pages/1",
              "templated": false,
              "rel": null
            },
            "previous": {
              "href": "/api/event-feed/pages/1",
              "templated": false,
              "rel": null
            },
            "self": {
              "href": "/api/event-feed/pages/2",
              "templated": false,
              "rel": null
            },
            "next": {
              "href": "/api/event-feed/pages/3",
              "templated": false,
              "rel": null
            },
            "tail": {
              "href": "/api/event-feed/pages/3",
              "templated": false,
              "rel": null
            },
            "page": {
              "href": "/api/event-feed/pages/{pageNumber}",
              "templated": true,
              "rel": null
            }
          },
          "isComplete": true
        }
        """

        let page = Page.Deserialize(json)

        Assert.Equal(2, page.pageNumber)
        Assert.Equal("/api/event-feed", page._links.meta.href)
        Assert.Equal("/api/event-feed/pages/1", page._links.head.href)
        Assert.Equal("/api/event-feed/pages/1", page._links.previous.Value.href)
        Assert.Equal("/api/event-feed/pages/3", page._links.next.Value.href)
        Assert.Equal("/api/event-feed/pages/2", page._links.self.href)
        Assert.Equal("/api/event-feed/pages/{pageNumber}", page._links.page.href)
        Assert.False(page._links.meta.templated)
        Assert.False(page._links.head.templated)
        Assert.False(page._links.previous.Value.templated)
        Assert.False(page._links.next.Value.templated)
        Assert.False(page._links.self.templated)
        Assert.False(page._links.tail.templated)
        Assert.True(page._links.page.templated)

