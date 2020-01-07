hover_effects_on = true;

function sortTable(id, column_number, type) {
    var table, rows, switching, i, x, y, shouldSwitch, dir = 0;
    table = document.getElementById(id);
    switching = true;
    // Set the sorting direction to ascending:
    dir = "asc";
    let headers = table.getElementsByTagName("TR")[0].getElementsByTagName("th");
    
    sorted = false
    if (headers[column_number].getAttribute('data-sortorder') == "asc") { dir = "desc"; sorted = true }
    if (headers[column_number].getAttribute('data-sortorder') == "desc") {sorted = true}

    for (var j = 0; j < headers.length; j++) {
        headers[j].setAttribute('data-sortorder', "");
    }
    headers[column_number].setAttribute('data-sortorder', dir);

    rows = Array.from(table.getElementsByTagName("TR"));
    values = [null]
    for (i = 1; i < rows.length; i++) {
        x = rows[i].getElementsByTagName("TD")[column_number]
        switch(type) {
            case "string": 
                values.push(x.innerHTML.toLowerCase())
                break
            case "number": 
                values.push(Number(x.innerHTML))
                break
            case "id": 
                var p = x.innerText.slice(1).split(":")
                values.push(Number(p[p.length - 1]))
                break
        }
    }

    if (sorted) {
        rows = rows.reverse()
        rows.splice(0, 0, rows[rows.length - 1])
        rows.pop()
        switching = false
    } else {
        switching = true
    }

    //console.log(dir, switching)

    while (switching) {
        switching = false;
        /* Loop through all table rows (except the
        first, which contains table headers): */
        for (i = 1; i < (rows.length - 1); i++) {
            shouldSwitch = false;
            if (values[i] > values[i+1]) {
                shouldSwitch = true;
                break;
            }
        }
        if (shouldSwitch) {
            el = rows[i + 1]
            rows.splice(i + 1, 1)
            rows.splice(i, 0, el)
            el = values[i + 1]
            values.splice(i + 1, 1)
            values.splice(i, 0, el)
            //rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
            switching = true;
        }
    }
    // Remove old rows and append all new rows
    while (table.lastChild) {
        table.removeChild(table.lastChild);
    }
    var frag = document.createDocumentFragment();
    for (var i = 0; i < rows.length; ++i) {
        frag.appendChild(rows[i]);
    }
    table.appendChild(frag);
}

window.onhashchange = function (ev) {
    var target = window.location.href.split("#")[1];

    var els = document.getElementsByClassName("selected")
    while (els[0]) {
        els[0].classList.remove('selected')
    }

    document.getElementById("table-" + target).classList.add("selected");
    if (target[0] == ContigPrefix) {
        document.getElementById("node-" + target).classList.add("selected");
        document.getElementById("simple-node-" + target).classList.add("selected");
    }
    if (target[0] == PathPrefix) {
        els = this.document.getElementsByClassName(target)
        for (let el of els) {
            if (el != null && el != undefined) el.classList.add("selected");
        }
    }
}

function Select(id) {
    window.location.href = "#" + id;
}

/*
function Select(prefix, number) {
    window.location.href = "#" + prefix + lpad(number.toString(), '0', 4);
}*/

function lpad(str, padString, length) {
    while (str.length < length)
        str = padString + str;
    return str;
}

var dragging = false;

function Setup() {
    document.getElementById("aside-handle").addEventListener('mousedown', function (ev) {
        dragging = true;
        document.body.classList.add("dragging");
        pauseEvent(ev);
    })

    var elements = document.getElementsByClassName("align-link");
    for (var i = 0; i < elements.length; i++) {
        elements[i].addEventListener('mouseover', enterHoverOver);
        elements[i].addEventListener('mouseout', exitHoverOver);
    }

    var elements = document.getElementById("aside").getElementsByClassName("read-link");
    for (var i = 0; i < elements.length; i++) {
        elements[i].addEventListener('mouseover', enterHoverOver);
        elements[i].addEventListener('mouseout', exitHoverOver);
    }
}

document.addEventListener('mousemove', function (ev) {
    if (dragging) {
        width = window.innerWidth - ev.pageX;
        document.getElementById("aside").style.flexBasis = width.toString() + "px";

        if (width < 200) {
            document.body.classList.add("only-main");
        } else {
            document.body.classList.remove("only-main");
        }

        if (ev.pageX < 200) {
            document.body.classList.add("only-aside");
        } else {
            document.body.classList.remove("only-aside");
        }

        pauseEvent(ev);
    }
})

document.addEventListener('mouseup', function () {
    dragging = false;
    document.body.classList.remove("dragging");
})

function pauseEvent(e) {
    if (e.stopPropagation) e.stopPropagation();
    if (e.preventDefault) e.preventDefault();
    e.cancelBubble = true;
    e.returnValue = false;
    return false;
}

function enterHoverOver(e) {
    if (!hover_effects_on) return;
    var id = e.target.href;
    var target = window.location.href.split("#")[1];
    var aside = document.getElementById(target);
    var elements = aside.getElementsByClassName("align-link");
    for (var i = 0; i < elements.length; i++) {
        if (elements[i].href == id) {
            elements[i].classList.add('hover');
        } else {
            elements[i].classList.remove('hover');
        }
    }
    var elements = aside.getElementsByClassName("read-link");
    for (var i = 0; i < elements.length; i++) {
        if (elements[i].href == id) {
            elements[i].classList.add('hover');
        } else {
            elements[i].classList.remove('hover');
        }
    }
}

function exitHoverOver(e) {
    var elements = document.getElementsByClassName("hover");
    while (elements.length > 0) {
        elements[0].classList.remove('hover');
    }
}

function togglejs() {
    hover_effects_on = !hover_effects_on;
}