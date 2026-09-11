Mesocyclones styling is very... uhhhh, *interesting* let's just say; and doesn't comply with most of the C# naming conventions.
So here's just a little styling guide for how the game is written ig.


"variable"
- When we mention a "variable" (which is traditionally used in other languages like python, lua, etc.), we're referring to a field and/or property. Here, even tho on the technical level is the contrary, a property is closer to field as it's just a field with logic when it's referenced and modified. That's our mindset

camelCase
* All type members on half bread's part.

PascalCase
* All types, *& members on Astraa's part*. And sometimes will use a blend between PascalCase and snake_case, so don't mind that :P
* half bread also uses this for members just to please Astraa for code they both will work on a lot (like DOTS)

SCREAMING_SNAKE_CASE
* *sometimes* constants... But usually for constant we use PascalCase

// comments
* if you want to be able to distinguish between a comment half bread and Astraa made:
    * half bread puts a space between the marker: `// hello,`.
    * And Astraa doesn't: `//scugs!!`
