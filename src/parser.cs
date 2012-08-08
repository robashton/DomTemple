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
        var node = document.DocumentNode.SelectSingleNode(xpath);
        if(node != null) {
          var value = (string)property.GetValue(model, null);
          node.InnerHtml = value;
        }
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
    public void Can_replace_title_by_convention() {
      var html = Parser.Process(
          "<html><head><title>Placeholder</title></head></html>",
          new { Title =  "My Page" });

      Assert.That(html.IndexOf("<title>My Page</title>") >= 0);
    }
   
    [Test]
    public void Can_replace_body_by_convention() {
      var html = Parser.Process(
          "<html><body></body></html>",
          new { Body = "<h1>Heaven is a place on earth</h1>" });

      Assert.That(html.IndexOf("<body><h1>Heaven is a place on earth</h1></body>") >= 0);

    }
  }
}
