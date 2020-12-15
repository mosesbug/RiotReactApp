import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Help } from './components/Help';
import { GameHistory } from './components/GameHistory';

import './custom.css'

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={GameHistory} />
        <Route path='/help' component={Help} />
      </Layout>
    );
  }
}
