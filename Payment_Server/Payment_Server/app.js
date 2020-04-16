const net = require('net');
const express = require('express')
const bodyParser = require('body-parser');
const fs = require('fs')
const logger = fs.createWriteStream('log.txt', { flags: 'a' })
const moment = require('moment')

function log(msg) {
	var output = moment().format("YYYY-MM-DD hh:mm:ss") + "," + msg + "\n"
	logger.write(output);
	console.log(output);
}

var events = {
	"1": {}
};
var callbacks = {
	"1": {}
}
function response_DS(work) {
	if (events[work.org_id][work.work_id] == undefined) { return }
	log("EXT," + JSON.stringify(work))
	callbacks[work.org_id][work.work_id](work.msg);
	delete events[work.org_id][work.work_id]
	delete callbacks[work.org_id][work.work_id]
}



var work_id = 0;
var server = net.createServer(function (socket) {
	socket.on('data', function (data) {
		log("DS," + data)
		var json = JSON.parse(data)
		var wid = work_id++;
		json.work_id = wid
		events[json.org_id][wid] = json
		callbacks[json.org_id][wid] = function (msg) {
			socket.write(JSON.stringify(msg))
			socket.end();
		}
		setTimeout(() => {
			response_DS({
				org_id: json.org_id,
				work_id: wid,
				msg: "Timeout"
            })
        }, 5000)
	});
});
server.listen(1101, '0.0.0.0');



var app = express();
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());
app.get('/show_work', function (req, res) {
	/* var resp = {}
	var count = 0
	for (var key in events[req.query.org_id]) {
		if (dictionary.hasOwnProperty(key)) resp[key] = events[req.query.org_id][key]
		if (count > 100) break
	}
	res.send(JSON.stringify(resp)); */
	res.send(JSON.stringify(events[req.query.org_id]))
});
app.post('/submit_work', function (req, res) {
	req.body.forEach((work) => { response_DS(work) })
	res.send("OK")
});
app.listen(5269, function () {
	log('Payment Server is now listening!');
});