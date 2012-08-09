using System;
using System.Text;
using HtmlAgilityPack;
using System.Reflection;
using NUnit.Framework;

namespace DomTemple {
  public class Parser {

    public static string Process(string input, object[] models) {
      var builder = new StringBuilder();
      for(var x = 0; x < models.Length; x++)
        builder.Append(Parser.Process(input, models[x]));
      return builder.ToString();
    }

    public static string Process(string input, object model) {
      var document = new HtmlDocument();
      document.LoadHtml(input);

      var found = false;

      foreach(var property in model.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance)) {

        var xpath = string.Format("//{0}", property.Name.ToLower());
        var value = (string)property.GetValue(model, null);
        var node = document.DocumentNode.SelectSingleNode(xpath);
        if(node != null) {
          node.InnerHtml = value;
          found = true;
        }

        xpath = string.Format("id('{0}')", property.Name.ToLower());
        var nodes = document.DocumentNode.SelectNodes(xpath);
        if(nodes != null) {
          foreach(var target in nodes)
            target.InnerHtml = value;
          found = true;
        }

        if(found) continue;

        xpath = string.Format("//*[@class='{0}']", property.Name.ToLower());
        nodes = document.DocumentNode.SelectNodes(xpath);
        if(nodes != null)
          foreach(var target in nodes)
            target.InnerHtml = value;
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
        .Expect("<html><h1 class=\"title\">Hello world</h1></html>");
    }

    [Test]
    public void Matching_by_id_means_not_matching_by_class() {
      new ParseTest()
        .Html("<html><head><title></title></head><body><h1 class=\"title\"></h1></body></html>")
        .Input(new { Title = "God save the queen" })
        .Expect("<html><head><title>God save the queen</title></head><body><h1 class=\"title\"></h1></body></html>");
    }


    [Test]
    public void Can_replace_items_within_a_fragment() {
      new ParseTest()
        .Html("<p class=\"name\"></p>")
        .Input(new { Name = "bob" })
        .Expect("<p class=\"name\">bob</p>");
    }

    [Test]
    public void Can_repeat_over_array_of_items() {
      new ParseTest()
        .Html("<p class=\"name\"></p>")
        .Input(new Object [] { 
           new { Name = "bob" },
           new { Name = "alice" }
         })
        .Expect("<p class=\"name\">bob</p><p class=\"name\">alice</p>");
    }

    [Test]
    public void Can_perform_templating_on_sub_objects() {
      new ParseTest()
        .Html("<h1 class=\"title\"></h1><div id=\"artist\"><p class=\"name\"></p></div>")
        .Input(new { 
          Title = "Artist",
          Artist = new { 
            Name = "Prince"
          }
        })
        .Expect("<h1 class=\"title\">Artist</h1><div id=\"artist\"><p class=\"name\">Prince</p></div>");
    }
  }

  public class ParseTest {
    string html;
    object input;
    object[] inputs;

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

    public ParseTest Input(object[] inputs) {
      this.inputs = inputs;
      return this;
    }

    public void Expect(string expected) {
      string html = string.Empty;
      if(this.inputs != null)
        html = Parser.Process(this.html, this.inputs);
      else
        html = Parser.Process(this.html, this.input);
      Assert.That(html, Is.EqualTo(expected));
    }
  }
}
