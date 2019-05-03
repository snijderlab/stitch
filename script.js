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

window.onhashchange = function(ev) {
    var target = window.location.href.split("#")[1];
    var number = Number(target.slice(1));

    console.log(target);

    var els = document.getElementsByClassName("selected")
    while (els[0]) {
        els[0].classList.remove('selected')
    }

    if (target[0] == 'I') {
        document.getElementById("node" + number).classList.add("selected");
        document.getElementById("simple-node" + number).classList.add("selected");
        document.getElementById("table-i" + number).classList.add("selected");
    } else if (target[0] == 'R') {
        document.getElementById("reads-table-r" + number).classList.add("selected");
    }
    console.log(number);
    console.log(document.getElementById("node" + number));
}

function Select(prefix, number) {
    window.location.href = "#" + prefix + lpad(number.toString(), '0', 4);
}

function lpad(str, padString, length) {
    while (str.length < length)
        str = padString + str;
    return str;
}