using InvoiceTrackerAPI2.Services;

namespace InvoiceTrackerAPI2.Tests.Services;

public class SanitizerTests
{
    // ── Text ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Text_StripsSingleHtmlTag()
    {
        Assert.Equal("Hello World", Sanitizer.Text("<b>Hello World</b>"));
    }

    [Fact]
    public void Text_StripsNestedHtmlTags()
    {
        Assert.Equal("Hello World", Sanitizer.Text("<div><span>Hello World</span></div>"));
    }

    [Fact]
    public void Text_TrimsWhitespace()
    {
        Assert.Equal("Hello", Sanitizer.Text("  Hello  "));
    }

    [Fact]
    public void Text_ReturnsEmptyForNullInput()
    {
        Assert.Equal(string.Empty, Sanitizer.Text(null));
    }

    [Fact]
    public void Text_ReturnsEmptyForWhitespaceOnlyInput()
    {
        Assert.Equal(string.Empty, Sanitizer.Text("   "));
    }

    // ── Name ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Name_StripsTags()
    {
        Assert.Equal("Acme Corp", Sanitizer.Name("<b>Acme Corp</b>"));
    }

    [Fact]
    public void Name_CollapsesInternalWhitespace()
    {
        Assert.Equal("Acme Corp", Sanitizer.Name("Acme   Corp"));
    }

    [Fact]
    public void Name_TrimsLeadingAndTrailingWhitespace()
    {
        Assert.Equal("Acme Corp", Sanitizer.Name("  Acme Corp  "));
    }

    [Fact]
    public void Name_ReturnsEmptyForNullInput()
    {
        Assert.Equal(string.Empty, Sanitizer.Name(null));
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Email_NormalisesToLowercase()
    {
        Assert.Equal("user@example.com", Sanitizer.Email("User@EXAMPLE.COM"));
    }

    [Fact]
    public void Email_StripsHtmlTags()
    {
        Assert.Equal("user@example.com", Sanitizer.Email("<script>user@example.com</script>"));
    }

    [Fact]
    public void Email_TrimsWhitespace()
    {
        Assert.Equal("user@example.com", Sanitizer.Email("  user@example.com  "));
    }

    [Fact]
    public void Email_ReturnsEmptyForNullInput()
    {
        Assert.Equal(string.Empty, Sanitizer.Email(null));
    }

    // ── NullableText ──────────────────────────────────────────────────────────

    [Fact]
    public void NullableText_ReturnsNullForNullInput()
    {
        Assert.Null(Sanitizer.NullableText(null));
    }

    [Fact]
    public void NullableText_ReturnsNullForWhitespaceOnlyInput()
    {
        Assert.Null(Sanitizer.NullableText("   "));
    }

    [Fact]
    public void NullableText_StripsTags()
    {
        Assert.Equal("Hello", Sanitizer.NullableText("<em>Hello</em>"));
    }

    [Fact]
    public void NullableText_TrimsWhitespace()
    {
        Assert.Equal("Hello", Sanitizer.NullableText("  Hello  "));
    }
}
