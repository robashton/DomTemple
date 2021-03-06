using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DomTemple.Tests {

  [TestFixture]
  public class BasicTemplating {

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
          Artists = new Artist [] {} 
        })
        .Expect("<ul id=\"artists\"></ul>");
    }
    
    [Test]
    public void Can_safely_ignore_empty_custom_object_properties() {
      new ParseTest()
        .Html("<ul id=\"country\"><li class=\"name\"></li></ul>")
        .Input(new TestViewModel {
          Country = null
        })
        .Expect("<ul id=\"country\"><li class=\"name\"></li></ul>");
    }

    [Test]
    public void Can_match_on_fully_specified_path() {
      new ParseTest()
        .Html("<p class=\"artist-name\"></p>")
        .Input(new { Artist = new { Name = "Marv" }})
        .Expect("<p class=\"artist-name\">Marv</p>");
    }
  }

  public class TestViewModel {
    public Country Country { get; set; }
    public Artist[] Artists { get; set; }
  }

  public class Country {
    public string Code { get; set; }
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
