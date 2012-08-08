using System;
using HtmlAgilityPack;
using System.Reflection;
using NUnit.Framework;

namespace DomTemple {
  public class Parser {
    public static string Process(string input, object model) {
      var document = new HtmlDocument();
      document.LoadHtml(input);


      foreach(var property in model.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance)) {

        var xpath = string.Format("//{0}", property.Name.ToLower());
        var value = (string)property.GetValue(model, null);
        var node = document.DocumentNode.SelectSingleNode(xpath);
        if(node != null) {
          node.InnerHtml = value;
        }

        xpath = string.Format("id('{0}')", property.Name.ToLower());
        var nodes = document.DocumentNode.SelectNodes(xpath);
        if(nodes != null)
          foreach(var needle in nodes)
            needle.InnerHtml = value;
      }

      return document.DocumentNode.WriteTo();
    }
  }
}


namespace DomTemple.Tests {
  [TestFixture]
  public class BasicFunctionality {

    [Test]
    public void Can_load_and_return_html() {
      var html = Parser.Process("<html></html>", new {});
      Assert.AreEqual("<html></html>", html);
    }

    [Test]
    public void Can_replace_title_element_by_convention() {
      var html = Parser.Process(
          "<html><head><title>Placeholder</title></head></html>",
          new { Title =  "My Page" });

      Assert.That(html.IndexOf("<title>My Page</title>") >= 0);
    }
   
    [Test]
    public void Can_replace_body_element_by_convention() {
      var html = Parser.Process(
          "<html><body></body></html>",
          new { Body = "<h1>Heaven is a place on earth</h1>" });

      Assert.That(html.IndexOf("<body><h1>Heaven is a place on earth</h1></body>") >= 0);
    }

    [Test]
    public void Can_replace_title_by_id_by_convention() {
      new ParseTest()
          .Html("<html><body><h1 id=\"title\"></h1></body></html>")
          .Input( new { Title = "Hello world" })
          .Expect("<html><body><h1 id=\"title\">Hello world</h1></body></html>");
    }


    [Test]
    public void Can_replace_title_by_id_and_body_by_convention() {
      new ParseTest()
        .Html("<html><head><title></title></head><body><h1 id=\"title\"></h1></body></html>")
        .Input( new { Title = "Hello world"})
        .Expect("<html><head><title>Hello world</title></head><body><h1 id=\"title\">Hello world</h1></body></html>");
    }

    [Test]
    public void Can_replace_title_by_class_by_convention() {
      new ParseTest()
        .Html("<html><h1 class=\"title\"></h1></html>")
        .Input(new { Title = "Hello world" })
        .Expect("<html><h1>Hello world</h1></html>");
    }
  }

  public class ParseTest {
    string html;
    object input;

    public ParseTest() {

    }

    public ParseTest Html(string html) {
      this.html = html;
      return this;
    }
    
    public ParseTest Input(object input) {
      this.input = input;
      return this;
    }

    public void Expect(string expected) {
      var html = Parser.Process(this.html, this.input);
      Assert.That(html, Is.EqualTo(expected));
    }
  }
}
