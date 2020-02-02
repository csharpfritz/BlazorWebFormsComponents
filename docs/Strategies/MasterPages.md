# MasterPage migration strategies

ASP<span></span>.NET MasterPages provide a way to define the shared layout for your application, and you can change MasterPage references with a MasterPage directive at the top of your Web Form.  In Blazor, the concept shifts slightly with Layout where you can now define a default layout per route and override the layout per page.

## Features not available in Layout

### HTML, HEAD, BODY are handled elsewhere 

The MasterPage in Web Forms was identified with its `.master` file extension and contained all of the outer HTML to be rendered for the page.  In Blazor, the layout and pages are hosted inside of a `_Host.cshtml` page or another page where a top-level `App` component is hosted.  This means that all of your CSS and JavaScript references are hosted in a parent page above the layout.

**Recommended Solution:** Separate the CSS and JavaScript references for your HTML and embed them in the host page (typically `_Host.cshtml` or `index.html`).  Place the content that would have appeared inside the HTML `BODY` element in a Layout page.

>NEED BEFORE AND AFTER SAMPLE

### Pages cannot access information about their Layout

In Web Forms, you can access the entire page hierarchy from anywhere else in the page or its child controls.  This capability is removed in favor of layouts and components able to share state between components using a model passed through the CascadingParameter feature