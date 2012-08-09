using System;
using System.Collections;
using System.Collections.Generic;
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

      foreach(var property in model.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance)) {
        var value = property.GetValue(model, null);

        if(IsBindable(property)) 
          BindStringToNode(document.DocumentNode, property.Name, value.ToString());
        else {

        }
      }

      return document.DocumentNode.WriteTo();
    }

    private static bool IsBindable(PropertyInfo property) {
      return property.PropertyType.IsPrimitive
        ||   property.PropertyType == typeof(string);
    }

    private static IEnumerable<HtmlNode> FindMatchingNodes(HtmlNode root, string name) {
      var found = false;
      var xpath = string.Format("//{0}", name.ToLower());

      var node = root.SelectSingleNode(xpath);
      if(node != null) {
        yield return node;
        found = true;
      }

      xpath = string.Format("id('{0}')", name.ToLower());
      var nodes = root.SelectNodes(xpath);
      if(nodes != null) {
        foreach(var target in nodes)
          yield return target;
        found = true;
      }

      if(found) yield break;

      xpath = string.Format("//*[@class='{0}']", name.ToLower());
      nodes = root.SelectNodes(xpath);
      if(nodes != null)
        foreach(var target in nodes)
          yield return target;
    }

    private static void BindStringToNode(HtmlNode root, string name, string value) {
      foreach(var target in FindMatchingNodes(root, name)) 
        target.InnerHtml = value;
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
