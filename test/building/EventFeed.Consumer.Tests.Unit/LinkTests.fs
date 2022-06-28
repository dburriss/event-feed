namespace EventFeed.Consumer.Tests

module LinkTests =

    open Xunit
    open EventFeed.Consumer

    let nonTemplated = { href = "https://test.com"; rel = Some "test"; templated = false}
    let nonTemplatedWithNoHole = { href = "https://test.com"; rel = Some "test"; templated = true}
    let nonTemplatedWithHole = { href = "https://test.com/{page}"; rel = Some "page"; templated = true}

    [<Fact>]
    let ``Non templated link retruns same href`` () =
        let href = Link.Render("doesnt-matter", nonTemplated)
        Assert.Equal(nonTemplated.href, href)

    [<Fact>]
    let ``Templated link with no hole retruns same href`` () =
        let href = Link.Render("doesnt-matter", nonTemplatedWithNoHole)
        Assert.Equal(nonTemplatedWithNoHole.href, href)

    [<Fact>]
    let ``Templated link with hole retruns href with value replaced`` () =
        let href = Link.Render(1, nonTemplatedWithHole)
        Assert.Equal("https://test.com/1", href)
