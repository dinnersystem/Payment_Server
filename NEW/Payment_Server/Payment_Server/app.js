const net = require('net');
const express = require('express')
const bodyParser = require('body-parser');
const fs = require('fs')
const logger = fs.createWriteStream('log.txt', { flags: 'a' })
const moment = require('moment')

function log(msg) {
	logger.write(moment().format("YYYY-MM-DD hh:mm:ss") + "," + msg + "\n");
	console.log(msg);
}


var events = {
	"1": {}
};
var callbacks = {
	"1": {}
}


var work_id = 0;
var server = net.createServer(function (socket) {
	socket.on('data', function (data) {
		var json = JSON.parse(data)
		var wid = work_id++;
		json.work_id = wid
		events[json.org_id][wid] = json
		callbacks[json.org_id][wid] = function (msg) {
			socket.write(JSON.stringify(msg))
			socket.end();
		}
		log("DS," + JSON.stringify(json))
	});
});
server.listen(1101, '0.0.0.0');



var app = express();
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());
app.get('/show_work', function (req, res) {
	var resp = {}
	var count = 0
	for (var key in events[req.query.org_id]) {
		if (dictionary.hasOwnProperty(key)) resp[key] = events[req.query.org_id][key]
		if (count > 100) break
	}
	res.send(JSON.stringify(resp));
});
app.post('/submit_work', function (req, res) {
	if (events[req.body.org_id][req.body.work_id] == undefined) { return }
	log("EXT," + JSON.stringify(req.body))
	callbacks[req.body.org_id][req.body.work_id](req.body.msg);
	delete events[req.body.org_id][req.body.work_id]
	delete callbacks[req.body.org_id][req.body.work_id]
	res.send("OK")
});
app.listen(5269, function () {
	log('Payment Server is now listening!');
});