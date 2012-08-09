DOM Temple
----------------

*Your DOM is a temple, stop polluting it with templating tags*


This is basically 'Plates' or 'Weld' from the node world (nearer to Weld because it currently parses the DOM instead of using Regex fu).

Consider

    public class Artist {
      public string Name { get; set; }
    }


And

   <p class="name"></p>

Given

    var artist = new Artist() { Name = "Prince" }
    var html = Parser.Process(html, artist);

We get

    <p class="name">Prince</p>


Told you it was pretty much 'Plates' or 'Weld'

Now, it's a *Work In Progress* which means I'm not releasing anything and I'm just playing around to see if I can make something like this not only work, but work in ASP.NET MVC as a view engine with practical use. 

It supports matching on tag name, id and class by convention, so

    <html>
      <head>
        <title></title>
      </head>
      <body>
        <h1 id="title"></h1>
        <h4 class="artist-name"></h4>
        <p class="artist-description"></p>
      </body>
    </html>


With

    public class ArtistViewModel {
      public string Title { get; set; }
      public Artist Artist { get; set; }
    }

And

    var model = new ArtistViewModel() {
      Title = "Showing you an artist",
      Artist = new Artist() {
        Name = "Prince",
        Description = "The artist formerly known as prince.."
      }
    }

    var html = Parser.Process(template, model);

Should result in

    <html>
      <head>
        <title>Showing you an artist</title>
      </head>
      <body>
        <h1 id="title">Showing you an artist</h1>
        <h4 class="artist-name">Prince</h4>
        <p class="artist-description">The artist formerly known as prince..</p>
      </body>
    </html>


*Reminder: It's a work in progress*

The above may not match what is currently in the repository, I'm currently playing around with conventions of things that make sense for things like nested complex objects/collections/etc - and will probably need to look at HTML building functions too (and re-use of micro-templates across the site for things like form controls or widgets)


I'll see how far I can push it with an example project, or if I get bored and then do something with it. Keeping the code live up here so others can fork it and do what they will with the learnings.
