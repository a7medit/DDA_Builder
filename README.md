<article class="markdown-body entry-content" itemprop="text">
<h3>Data driven code generator</h3>

<h3>About: </h3>
this tool will allow you to use Razor template to generate customized code, with the way you like,
imgaine you can generate code for mobile,web or even desktop Apps in a simple click, all you need to do is creating your own Razor template,
and your own database (SQL server only for now).


<h3>how it works:</h3>

<img src="https://s9.postimg.org/q9tr46kfj/code_generator_diagram.jpg" alt="Smiley face" >

</article>

<h3>Getting Started</h3>

1 :Create a Razor template : 
  
  example Razor template for generating an HTML table from your data model
  
```razor
<table>
<thead>
<tr>
 @foreach (var item in @Model.TableDefination.Rows)
 {
 <th> @(item["Field"]) </th>
 }
</tr>
</thead>
<tbody>
<tr>
 @foreach (var item in @Model.TableDefination.Rows)
  {
<td> </td>
}
</tr>
</tbody>
</table>
  ```



<h3>things to do: </h3>
<ul>
  <li>Razor exception handling</li>
  <li>Template Edit/Add</li>
</ul>
  

