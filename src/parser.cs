using System;
using System.Linq;
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
      ProcessNode(document.DocumentNode, model);
      return document.DocumentNode.WriteTo();
    }

    private static void ProcessNode(HtmlNode node, Object model) {
      foreach(var property in model.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance)) {

        var value = property.GetValue(model, null);

        if(IsBindable(property.PropertyType)) 
          BindStringToNode(node, property.Name, value.ToString());
        else if(IsArray(property.PropertyType))
          BindArrayToNode(node, property.Name, (object[])value);
        else {
          foreach(var target in FindMatchingNodes(node, property.Name))
            ProcessNode(target, value);
        }
      }
    }

    private static bool IsBindable(Type type) {
      return type.IsPrimitive
        ||   type == typeof(string);
    }

    private static bool IsArray(Type type) {
      return type.IsArray;
    }

    private static void BindArrayToNode(HtmlNode node, string name, object[] items) {
      foreach(var container in FindMatchingNodes(node, name)) {
        var template = container.SelectSingleNode("./*[@class='template']")  
          ?? container.ChildNodes[0]; 

        ClearTemplateClassFrom(template);
        template.Remove();

        if(items == null || items.Length == 0) continue;

        for(var x = 0; x < items.Length; x++) {
          var item = items[x];
          var target = template.CloneNode(true);
          container.ChildNodes.Append(target);

          var type = item.GetType();

          if(IsBindable(type))
            target.InnerHtml = (string)item;
          else 
            ProcessNode(target, item);
        }
      }
    }

    private static void ClearTemplateClassFrom(HtmlNode node) {
      var templateClass = node.Attributes.AttributesWithName("class").FirstOrDefault();
      if(templateClass == null) return;


      templateClass.Value = templateClass.Value.Replace("template", "").Trim();
      if(templateClass.Value == string.Empty)
        node.Attributes.Remove(templateClass);
    }

    private static IEnumerable<HtmlNode> FindMatchingNodes(HtmlNode root, string name) {
      var found = false;
      var xpath = string.Format(".//{0}", name.ToLower());
      
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

      xpath = string.Format(".//*[@class='{0}']", name.ToLower());
      nodes = root.SelectNodes(xpath);
      if(nodes != null)
        foreach(var target in nodes)
          yield return target;
    }

    private static void BindStringToNode(HtmlNode root, string name, string value) {
      foreach(var target in FindMatchingNodes(root, name)) { 
        target.InnerHtml = value;
      }
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

    [Test]
    public void Can_perform_templating_on_primitive_array_properties() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li></li></ul>")
        .Input(new {
          Artists = new [] { "bob", "alice", "james" } 
        })
        .Expect("<ul id=\"artists\"><li>bob</li><li>alice</li><li>james</li></ul>");
    }

    [Test]
    public void Can_perform_templating_on_object_array_properties() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li><p class=\"name\"></p></li></ul>")
        .Input(new {
           Artists = new [] {
             new { Name = "bob" },
             new { Name = "alice" }
           }
         })
        .Expect("<ul id=\"artists\"><li><p class=\"name\">bob</p></li><li><p class=\"name\">alice</p></li></ul>");
    }

    [Test]
    public void Can_use_an_explicit_template_for_primitive_array_properties() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li class=\"header\"></li><li class=\"template\"></li></ul>")
        .Input(new {
          Artists = new [] { "bob", "alice", "james" } 
        })
        .Expect("<ul id=\"artists\"><li class=\"header\"></li><li>bob</li><li>alice</li><li>james</li></ul>");
    }

    [Test]
    public void Can_use_an_explicit_template_for_object_array_properties() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li class=\"header\"></li><li class=\"template\"><p class=\"name\"></p></li></ul>")
        .Input(new {
           Artists = new [] {
             new { Name = "bob" },
             new { Name = "alice" }
           }
         })
        .Expect("<ul id=\"artists\"><li class=\"header\"></li><li><p class=\"name\">bob</p></li><li><p class=\"name\">alice</p></li></ul>");
    }


    [Test]
    public void Can_safely_ignore_null_array_property_values() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li></li></ul>")
        .Input(new TestViewModel {
          Artists = null
        })
        .Expect("<ul id=\"artists\"></ul>");

    }
    [Test]
    public void Can_safely_ignore_empty_array_property_values() {
      new ParseTest()
        .Html("<ul id=\"artists\"><li></li></ul>")
        .Input(new TestViewModel {
          Artists = new [] {} 
        })
        .Expect("<ul id=\"artists\"></ul>");
    }
  }

  public class TestViewModel {
    public Artist[] Artists { get; set; }
  }

  public class Artist { 
    public string Name { get; set; }
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
