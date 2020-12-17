import React, { Component } from 'react';
import { Link } from 'react-router-dom';

export class Help extends Component {
  static displayName = Help.name;

  render () {
    return (
      <div className="center-home">
        <h2 className="center-title">To help you get started...</h2>
        <p>If running from development mode, <strong>add your Riot API key</strong> as a machine environment variable under the name <code>X-Riot-Token</code></p>
        <p><strong>Navigate to </strong><Link className="text-blue" to="/">Game History</Link> to browse your League match history</p>
        <div className="home-image"><img src="https://media.giphy.com/media/3oKIP73vEZmJjFNXtC/giphy.gif"></img></div>
      </div>


    );
  }
}
