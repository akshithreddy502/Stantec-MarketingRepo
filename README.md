1-Can you research and learn something new that you might not be entirely familiar with?
I enjoy learning new skills and information. For example, I recently learned how to use Lucene.NET to create a search engine. This involved reviewing the documentation, determining its functionality, then implementing it on a project.


2-Can you manage your time effectively to deliver something of value?
I set deadlines and plan my work so that I may be sure to complete tasks on time. I prioritize work based on deadlines and significance, breaking down more complicated tasks into smaller, more manageable segments to help me stay on schedule.


3-Can you design something that is easy-to-use and visually pleasing?
My main goal is to make designs that are both both beautiful and easy to use for users. For instance, I created a responsive header for a web application to make sure it was user-friendly and looked nice across all platforms.


4-Can you use version control correctly?
I organize my code using version control tools such as Git. I use branches for new features and fixes, merge changes carefully to prevent conflicts, and commit changes on a regular basis with meaningful messages.


5-Can you follow instructions and meet scope requirements?
I carefully read the project specifications and guidelines and follow them. I always make sure I understand the scope of the work before starting, and I double-check my work against the specs to make sure everything is covered. For instance, I added features like spell check and autocomplete to a project by following the directions.





Features Implemented:

1-Display Voting Average in the returned search results:
I updated the search results page to include the vote average. This entailed obtaining the vote average from the dataset and incorporating it into the user's search results display.



2-Finsh conecting the Voting Average (Minimum) so that it filters the results that are below the minimum selected values.
I implemented a filter that allows users to specify the required minimum vote average. Subsequently, the search results are filtered to display those movies that either meet or surpass this minimum vote average.



3-Add Release Date to the index and display it in the returned search results. (:bulb: Hint: You will need to reload the index after making changes to the indexing code.)
I added the release date to the Lucene index to ensure it is indexed alongside other movie data. In addition, I updated the search results display with each movie's premiere date.



4-Add a way to filter the search by date range for Release Date.
Thanks to a date range filter I made, users may now choose the start and end dates for the release date. The search results will only show movies that were released in the two weeks prior to these dates.



5-Show off your css skills - improve the styling and layout of the page and/or search results.
I improved the page's overall appearance and layout, including the way the search results are displayed. This required utilizing CSS to improve the interface's aesthetics and responsiveness while also making it more aesthetically pleasing and user-friendly.


6-Autocomplete - suggest search terms as the user types in the search box.
I made an autocomplete function that suggests search terms while the user types in the search box. It was necessary to construct an endpoint that provides potential search terms based on user input in order to show these concepts dynamically on the front end.



7-Stemming - when searching for “engineer”, the search should also return results for “engineering”, “engineers”, and “engineered”.
To make sure that different word variations are taken into account in the search results, I included a stemming mechanism. This implies that phrases like "engineering," "engineers," and "engineered" will also come up when searching for "engineer."



8-Spell checking - present corrected search terms for user misspellings.
In the event that an error occurs, I modified the spell-checking feature to offer users suggested corrected search phrases. This necessitated adding a spell checker that uses user input to find terms that match dictionaries the closest.