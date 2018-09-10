var myApp;


$(document).ready(function () {
	myApp = myApp || (function () {
		var pleaseWaitDiv = $('#pleaseWaitDialog');
		return {
			showPleaseWait: function () {
				pleaseWaitDiv.modal('show');
			},
			hidePleaseWait: function () {
				pleaseWaitDiv.modal('hide');
			}
		};
	})();

	$('#table-stats').DataTable({
		"order": [[2, "desc"]],
		"columnDefs": [
            { type: "file-size", targets: 2 }
		]
	});

	$('.thread-link').on('click', function () {
		$('a[href="#thread-report"]').removeClass('collapsed').attr('aria-expanded', 'true');
		$('#thread-report').addClass("in").attr('aria-expanded', 'true');
		$('#thread-report').removeAttr('style');
	});

	$('#closeall').click(function () {
		$('.thread-div.in')
			.collapse('hide');
	});
	$('#openall').click(function () {
		$('.thread-div:not(".in")')
			.collapse('show');
	});

	makeAllSortable(document);

	// make "copy stacktrace" button work
	var clipboard = new Clipboard('.copyButton');
	clipboard.on('success', function (e) {
		console.info('Text:', e.text);
		e.clearSelection();
		$(".copyButton").notify("copied to clipboard", "success");
	});
	// make "copy stacktrace" button work
	var clipboardForJira = new Clipboard('.copyButtonForJira');
	$('.copyButtonForJira').click(function (e) {
		$(".hidden-stacktrace-hack").show();
	});
	clipboardForJira.on('success', function (e) {
		console.info('Text:', e.text);
		$(".hidden-stacktrace-hack").hide();
		e.clearSelection();
		$(".copyButtonForJira").notify("copied to clipboard", "success");
	});
	

	$("input:checkbox.showaddresses").click(function () {
		var tablename = "#thread-" + $(this).attr("name") + "-stacktrace";
		$(tablename + " .stacktype").toggle();
		$(tablename + " .stackptr").toggle();
		$(tablename + " .stackinstructionptr").toggle();
		$(tablename + " .stackreturnaddr").toggle();

	});
	$("input:checkbox.showstackptroffset").click(function () {
		var tablename = "#thread-" + $(this).attr("name") + "-stacktrace";
		$(tablename + " .stackptroffset").toggle();
	});
	$("input:checkbox.showstackvars").click(function () {
		var tablename = "#thread-" + $(this).attr("name") + "-stacktrace";
		$(tablename + " .stackvariables").toggle();
	});

	$("input:checkbox.showsourceinfo").click(function () {
		var tablename = "#thread-" + $(this).attr("name") + "-stacktrace";
		$(tablename + " .sourceinfo").toggle();
	});
});

function openTabs(tabclass) {
	$('.thread-div.' + tabclass)
		.collapse('show');
}

function isScrolledIntoView(elem) {
	var docViewTop = $(window).scrollTop();
	var docViewBottom = docViewTop + $(window).height();

	var elemTop = $(elem).offset().top;
	var elemBottom = elemTop + $(elem).height();

	return ((elemBottom <= docViewBottom) && (elemTop >= docViewTop));
}
function formatBytes(bytes, decimals) {
	if (bytes === 0) return '0 Byte';
	var k = 1024;
	var dm = decimals + 1 || 3;
	var sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
	var i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}
function readURL(input) {
	if (input.files && input.files[0]) {
		$('.file-upload-btn').hide();
		$('.file-upload-wrap').hide();
		$('.file-upload-content').show();
		$('.file-title').html(input.files[0].name);

	} else {
		removeUpload();
	}
}
function startUpload() {
	var file = $('#file-selector')[0].files[0];
	uploadFile(file);
}

function uploadFile(file) {
	var xhr = new XMLHttpRequest();    // den AJAX Request anlegen
	myApp.showPleaseWait(); //show dialog
	xhr.upload.addEventListener("progress", progressHandler, false);
	xhr.addEventListener("load", completeHandler, false);

	xhr.onreadystatechange = function () {
		if (this.readyState === 4) {
			if (this.status === 200) {
				window.location.replace(xhr.responseURL);
			}
			else {
				document.open();
				document.write("<h1>Error status: " + xhr.status + "</h1>");
				document.write("<p>" + xhr.responseText + "</p>");
				document.close();
			}
		}
	}
	/*xhr.upload.onprogress = function (e) {
		if (e.lengthComputable) {
			var perc = (e.loaded / e.total) * 100;
			document.open();
			document.write('<div class="text-xs-center" id="example-caption-1">Uploading file&hellip; '+perc+'%'+'</div>');
			document.write('<progress style="width:100%;" class="progress progress-striped" value="'+perc+'" max="100"></progress>');
			document.close();
		}
	};*/
	xhr.open('POST', '/Home/Upload');    // Angeben der URL und des Requesttyps

	var formdata = new FormData();    // Anlegen eines FormData Objekts zum Versenden unserer Datei
	formdata.append('__RequestVerificationToken', $(".af-token input").val());
	formdata.append('file', file);  // Anhängen der Datei an das Objekt
	formdata.append('refurl', $('#refurl').val());
	formdata.append('note', $('#note').val());
	xhr.send(formdata);
}
function progressHandler(event) {
	var percent = (event.loaded / event.total) * 100;
	$('.progress-bar').attr('aria-nowvalue', percent);
	$('.progress-bar').html(Math.round(percent) + '%');
	$('.progress-bar').width(percent + '%'); //from bootstrap bar class
}

function completeHandler() {
	myApp.hidePleaseWait(); //hide dialog
	$('.progress-bar').attr('aria-nowvalue', 100);
	$('.progress-bar').width(100 + '%');
}

function callondrop(e) {
	e.preventDefault();
	if (e.dataTransfer && e.dataTransfer.files) {
		uploadFile(e.dataTransfer.files[0]);
	}
	return false;
}

function removeUpload() {
	$('.file-upload-input').replaceWith($('.file-upload-input').clone());
	$('.file-upload-content').hide();
	$('.file-upload-wrap').show();
	$('.file-upload-btn').show();
}
$('.file-upload-wrap').bind('dragover', function () {
	$('.file-upload-wrap').addClass('file-dropping');
});
$('.file-upload-wrap').bind('dragleave', function () {
	$('.file-upload-wrap').removeClass('file-dropping');
});


// from http://stackoverflow.com/questions/14267781/sorting-html-table-with-javascript
function makeSortable(table) {
	var th = table.tHead, i;
	th && (th = th.rows[0]) && (th = th.cells);
	if (th) i = th.length;
	else return; // if no `<thead>` then do nothing
	while (--i >= 0) (function (i) {
		var dir = 1;
		th[i].addEventListener('click', function () { sortTable(table, i, (dir = 1 - dir)) });
	}(i));
}
function makeAllSortable(parent) {
	parent = parent || document.body;
	var t = parent.getElementsByTagName('table'), i = t.length;
	while (--i >= 0) makeSortable(t[i]);
}
function sortTable(table, col, reverse) {
	var tb = table.tBodies[0], // use `<tbody>` to ignore `<thead>` and `<tfoot>` rows
        tr = Array.prototype.slice.call(tb.rows, 0), // put rows into array
        i;
	reverse = -((+reverse) || -1);
	tr = tr.sort(function (a, b) { // sort rows
		return reverse // `-1 *` if want opposite order
            * (a.cells[col].textContent.trim() // using `.textContent.trim()` for test
                .localeCompare(b.cells[col].textContent.trim())
               );
	});
	for (i = 0; i < tr.length; ++i) tb.appendChild(tr[i]); // append each row in order
}
// sortTable(tableNode, columId, false);

function tableToClipboard(table) {
	var txt = "";
	table.find('tr').each(function (i, el) {
		var $tds = $(this).find('td'),
            type = $tds.eq(0).text(),
            stackptr = $tds.eq(1).text(),
            instrptr = $tds.eq(2).text(),
            retaddr = $tds.eq(3).text(),
            methodname = $tds.eq(4).text();
		txt += methodname + "\n";
	});
	new Clipboard(table);
}