import React, { Component } from 'react';
import { Link } from 'react-router-dom';

export class Help extends Component {
  static displayName = Help.name;

  render () {
    return (
      <div className="center-home">
        <p>To help you get started:</p>
        <p><strong>Add your Riot API key</strong> as a machine environment variable under the name <code>X-Riot-Token</code></p>
        <p><strong>Navigate to </strong><Link className="text-blue" to="/">Game History</Link></p>
        <div className="home-image"><img src="https://media.giphy.com/media/3oKIP73vEZmJjFNXtC/giphy.gif"></img></div>
      </div>


    );
  }
}
