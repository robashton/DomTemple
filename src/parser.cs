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
      if(model == null) return;

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

