
    function showSuggestions(value) {
            if (value.length === 0) {
        closeAllLists();
    return;
            }

    $.ajax({
        type: "get",
    url: "/home/autocomplete?term=" + encodeURIComponent(value),
    contentType: "application/json; charset=utf-8",
    dataType: "json",
    success: function (data) {
        closeAllLists();
    var autocompleteList = document.getElementById("autocomplete-list");
    if (!autocompleteList) {
        autocompleteList = document.createElement("div");
    autocompleteList.setAttribute("id", "autocomplete-list");
    autocompleteList.setAttribute("class", "autocomplete-items");
    document.querySelector(".autocomplete").appendChild(autocompleteList);
                    }
    for (var i = 0; i < data.length; i++) {
                        var item = document.createElement("div");
    item.innerHTML = "<strong>" + data[i].substr(0, value.length) + "</strong>";
    item.innerHTML += data[i].substr(value.length);
    item.innerHTML += "<input type='hidden' value='" + data[i] + "'>";

        item.addEventListener("click", function (e) {
            document.getElementById("searchtext").value = this.getElementsByTagName("input")[0].value;
        closeAllLists();
                        });

        autocompleteList.appendChild(item);
                    }
                },
        failure: function (data) {
            console.error("Failed to fetch autocomplete suggestions");
                },
        error: function (data) {
            console.error("Error fetching autocomplete suggestions");
                }
            });
        }

        function closeAllLists(element) {
            var items = document.getElementsByClassName("autocomplete-items");
        for (var i = 0; i < items.length; i++) {
                if (element != items[i] && element != document.getElementById("searchtext")) {
            items[i].parentNode.removeChild(items[i]);
                }
            }
        }

        document.addEventListener("click", function (e) {
            closeAllLists(e.target);
        });
        document.addEventListener('DOMContentLoaded', function () {

            function showLoadingOverlay() {
                $(".loading-overlay").fadeIn();
            }

        function hideLoadingOverlay() {
            $(".loading-overlay").fadeOut();
        }

        // Number of results rows to send back at a time (10).
        var currentPage = 0;
        var rowsPerPage = 10;
        var rowCount = 0;

        // Encode the results
        var $converter = $("<div>");
            function htmlEncode(s) {
            return $converter.text(s).html();
        }

            // If they press the enter key, execute the search
            $("#searchtext").on("keydown", function (e) {
            var currentPage = 0;
            if (e.keyCode == 13) {
                e.preventDefault();
            updateSearchAndResetCount();
            }
        });

            // Initial seach button click (returns the intial set of results).
            $("#searchbutton").on("click", function () {
                updateSearchAndResetCount();
        });

            function updateSearchAndResetCount() {
                currentPage = 0;
            updateSearch();
        }

            $("#durationMinimumFacet, #durationMaximumFacet, #voteAverageMinimumFacet,#releaseDateStart,#releaseDateEnd").on("change", function () {
                updateSearchAndResetCount();
        });
            updateSearchAndResetCount();

            function updateSearch() {
                showLoadingOverlay();
            $.ajax({
                type: "get",
            url: "/home/search?searchString=" + encodeURIComponent($("#searchtext").val()) + "&start=" + currentPage + "&rows=" + rowsPerPage,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: {
                voteAverageMinimum: document.getElementById("voteAverageMinimumFacet").value,
            durationMinimum: document.getElementById("durationMinimumFacet").value,
            durationMaximum: document.getElementById("durationMaximumFacet").value,
            releaseDateStart: document.getElementById("releaseDateStart").value,
            releaseDateEnd: document.getElementById("releaseDateEnd").value
                },
            success: function (ajaxResponse) {
                console.log(ajaxResponse);
            rowCount = ajaxResponse.searchResults.recordsCount;
            var startRow = currentPage * rowsPerPage;
            var calculatedEndRow = (currentPage + 1) * rowsPerPage;
            var endRow = rowCount < calculatedEndRow ? rowCount : calculatedEndRow;
            $("#showing-records").empty();
            $("#showing-records").append("<em>Showing records " + startRow + "  to " + endRow + " out of " + rowCount + " </em>");

            addResults(0, ajaxResponse.searchResults.films);
            hideLoadingOverlay();
                    // Highlight: Show spell check suggestions if available
                    if (ajaxResponse.searchResults.suggestions && ajaxResponse.searchResults.suggestions.length > 0) {
                showSpellCheckSuggestions(ajaxResponse.searchResults.suggestions);
                    } else {
                hideSpellCheckSuggestions();
                    }
                },
            failure: function (ajaxResponse) {
                document.getElementById("errortext").textContent = "Failure! " + ajaxResponse.responseText;
                },
            error: function (ajaxResponse) {
                document.getElementById("errortext").textContent = "Error! " + ajaxResponse.responseText;
                }
            });
        }

            // Apply formatting and append new results.
            function addResults(start, results) {
                var lines = [];
                var resultCount = results.length;

                for (var i = 0; i < resultCount; ++i) {
                    var item = results[i];
                    lines.push("<div class='search-result'>");
                    lines.push("<div class='title'><a href='https://www.imdb.com/title/" + htmlEncode(item.id) + "'>" + htmlEncode(item.title) + "</a></div>");
                    lines.push("<div class='details'>");
                    if (item.runtime) lines.push("<span>" + item.runtime + " minutes</span>");
                    if (item.runtime && item.revenue) lines.push(" | ");
                    if (item.revenue) lines.push("<span>" + item.revenue.toLocaleString("en-US", { style: "currency", currency: "USD" }) + "</span>");
                    lines.push("</div>");
                    if (item.releaseDate) lines.push("<div class='details'>Initially released " + new Date(item.releaseDate).toLocaleDateString() + "</div>");
                    if (item.overview) lines.push("<div class='overview'>" + item.overview + "</div>");
                    if (item.voteAverage) lines.push("<div class='vote-average'>Vote Average: " + item.voteAverage.toFixed(1) + "</div>");
                    lines.push("<hr>");
                    lines.push("</div>");
                }

                $("#searchresults").empty();
                $("#searchresults").append(lines.join(""));
            }


        function showSpellCheckSuggestions(suggestions) {
                if (suggestions.length > 0) {
                    var suggestionHtml = "<div class='alert alert-warning' role='alert'><p>No results found matching your criteria. Did you mean: ";
            for (var i = 0; i < suggestions.length; i++) {
                suggestionHtml += "<a href='javascript:void(0)' onclick='updateSearchWithSuggestion(\"" + suggestions[i] + "\")'>" + suggestions[i] + "</a>&nbsp;";
                    }
            suggestionHtml += "</p></div>";
        $("#spell-check-suggestions").html(suggestionHtml).show();
                } else {
            $("#spell-check-suggestions").hide();
                }
            }



        function hideSpellCheckSuggestions() {
            $("#spell-check-suggestions").hide();
            }

        function updateSearchWithSuggestion(suggestion) {
            $("#searchtext").val(suggestion);
        updateSearchAndResetCount();
            }

        $("#previous-button").on("click", function () {
            if (currentPage > 0) {
            currentPage--;
        updateSearch();
        $("#nextPage").prop("disabled", false);
            }
        if (currentPage === 1) {
            $("#previousPage").prop("disabled", true);
            }
        });

        $("#next-button").on("click", function () {
            if (rowCount >= (currentPage + 1) * rowsPerPage) {
            currentPage++;
        updateSearch();
        $("#previousPage").prop("disabled", false);
            }
        else {
            $("#nextPage").prop("disabled", true);
            }
        });

        document.addEventListener('DOMContentLoaded', function () {
            hideLoadingOverlay
            hideSpellCheckSuggestions(); // Hide suggestions on initial load

        }, false);


        hideLoadingOverlay();
        });

