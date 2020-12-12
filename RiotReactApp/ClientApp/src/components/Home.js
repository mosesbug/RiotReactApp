import React, { Component } from 'react';
import { Link } from 'react-router-dom';

export class Home extends Component {
  static displayName = Home.name;

  render () {
    return (
      <div>
        <p>To help you get started:</p>
        <ul>
            <li><strong>Add your Riot API key</strong> as a machine environment variable under the name <code>X-Riot-Token</code></li>
            <li><strong>Navigate to </strong><Link className="text-blue" to="/game-history">Game History</Link></li>
        </ul>
            <h1 className="text-blue text-big-fun-font spaced-text"><strong>Welcome to your favorite League match history service</strong></h1> 
      </div>


    );
  }
}
