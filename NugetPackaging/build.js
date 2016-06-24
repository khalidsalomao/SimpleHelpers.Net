#!/usr/bin/env node

'use strict';

/**
 * Module dependencies.
 */

const program = require('commander');
const spawn = require('child_process').spawn; // https://nodejs.org/api/child_process.html#child_process_child_process_spawn_command_args_options
const readline = require('readline');
const fs = require('fs');
const _ = require("lodash"); // https://github.com/epeli/underscore.string
const async = require ('async');

/**
 * console options
 */
program
  .version('1.0.0')
  .option('-c, --config [./build_config.json]', 'Specify the json config file with build targets', './build_config.json')
  .option('-t, --target []', 'Specify list projects separeted by comma, defaulting to []', v => v.split(','), [])
  .option('--all', 'Run for all projects, this flag will ignore --target')
  .parse(process.argv);

console.log('parameters:');
console.log('  - config: ' + program.config);
console.log('  - target: ' + program.target);
console.log('  - all: ' + !!program.all);

/**
 * load config
 */
const config = {
    _opts: {},
    keys: [],
    load: function(filename) {
        if (!filename) throw new Error("Invalid config file");
        // load
        this._opts = require(filename);
        // prepare dictionary
        let obj = this._opts;
        // sanity check
        if (!obj) throw new Error("Invalid config file: " + filename);
        // normalize dictionary entries
        this.keys = [];
        for(var p in obj){
            if(obj.hasOwnProperty(p)){
                let v = obj[p];
                delete obj[p];
                p = p.toLowerCase();
                obj[p] = v;
                this.keys.push(p);
            }
        }
    },
    get: function(target) {
        return this._opts[target.toLowerCase()];
    }
};

config.load(program.config);
console.log('  - config keys: ' + config.keys);

let targets = program.target;
if (program.all || !targets || !targets.length){
    targets = config.keys;
}

/**
 * run app
 */
const fileProcessor = {
    execute: function (target, callback) {
        // sanity check
        if (!target || !target.source) {
            callback();
            return;
        }

        var namespaceDetected = false;
        var reader = readline.createInterface({ input: fs.createReadStream(target.source, { encoding: 'utf-8', highWaterMark: 16 * 1024 }) });
        var writer = fs.createWriteStream(target.dest, { defaultEncoding: 'utf8', highWaterMark: 16 * 1024 });

        reader.on('line', (l) => {
            if (!namespaceDetected && l.indexOf('namespace SimpleHelpers') === 0) {
                l = 'namespace $rootnamespace$.SimpleHelpers';
            }
            writer.write(l);
            writer.write('\r\n');
        });

        reader.on('close', () => writer.end());
        writer.on('finish', callback);
    },
    error: function (err) {
        console.error(err);
    }
};

// run!!!
console.info(targets);
targets = _.chain(targets).map(i => config.get(i)).filter(i => i.source && i.dest).value();
console.info(targets);
async.each(targets, fileProcessor.execute, fileProcessor.error);


// run nuget
// const nuget = spawn('nuget.exe', ['', '']);
// nuget.stdout.on('data', (data) => console.log(`stdout: ${data}`));
// nuget.stderr.on('data', (data) => console.log(`stderr: ${data}`));
// nuget.on('close', (code) => console.log(`child process exited with code ${code}`));
