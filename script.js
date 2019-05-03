function sortTable(id, column_number, type) {
    var table, rows, switching, i, x, y, shouldSwitch, dir, switchcount = 0;
    table = document.getElementById(id);
    switching = true;
    // Set the sorting direction to ascending:
    dir = "asc";
    let headers = table.getElementsByTagName("TR")[0].getElementsByTagName("th");
    for (var j = 0; j < headers.length; j++) {
        headers[j].className = "";
    }
    headers[column_number].className = "asc";

    while (switching) {
        switching = false;
        rows = table.getElementsByTagName("TR");
        /* Loop through all table rows (except the
        first, which contains table headers): */
        for (i = 1; i < (rows.length - 1); i++) {
            shouldSwitch = false;

            x = rows[i].getElementsByTagName("TD")[column_number];
            y = rows[i + 1].getElementsByTagName("TD")[column_number];

            var x_value = type == "string" ? x.innerHTML.toLowerCase() : type == "number" ? Number(x.innerHTML) : type == "id" ? Number(x.children[0].innerHTML.slice(1)) : null;
            var y_value = type == "string" ? y.innerHTML.toLowerCase() : type == "number" ? Number(y.innerHTML) : type == "id" ? Number(y.children[0].innerHTML.slice(1)) : null;
            if (dir == "asc") {
                if (x_value > y_value) {
                    shouldSwitch = true;
                    break;
                }
            } else if (dir == "desc") {
                if (x_value < y_value) {
                    shouldSwitch = true;
                    break;
                }
            }
        }
        if (shouldSwitch) {
            rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
            switching = true;
            switchcount++;
        } else {
            if (switchcount == 0 && dir == "asc") {
                dir = "desc";
                headers[column_number].className = "dsc";
                switching = true;
            }
        }
    }
}