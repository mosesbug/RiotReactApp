import React, { Component } from 'react';

export class FetchData extends Component {
  static displayName = FetchData.name;

    constructor(props) {
        super(props);
        this.state = { forecasts: [], loading: true };
    }

    componentDidMount() {
        this.populateWeatherData();
    }


    renderPage(forecasts) {
        return (
            <div>
                { this.renderEntryForm() }
                { this.renderForecastsTable(forecasts) }
            </div>
        );
    }

    renderEntryForm() {

        const options = [
            'NA', 'EUW', 'EUNE', 'KR', 'LAN'
        ];

        return (
            <div>
                <form onSubmit={this.onEntryFormSubmit}>
                    <p><input type='text' placeholder='Summoner Name' name='SummonerName' /> { this.getRegionSelect() }</p>
                    <p><button>Get Match History</button></p>
                </form>
            </div>
        );
    }

    onSelectRegion() {
        // get the current region selection and store it in a property
    }

    onEntryFormSubmit() {
        // get the summoner name, validate it on the server (character length and no special characters) and then search Riots API and update the table below
    }

    renderForecastsTable(forecasts) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
            <thead>
                <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
                </tr>
            </thead>
            <tbody>
                {forecasts.map(forecast =>
                <tr key={forecast.date}>
                    <td>{forecast.date}</td>
                    <td>{forecast.temperatureC}</td>
                    <td>{forecast.temperatureF}</td>
                    <td>{forecast.summary}</td>
                </tr>
                )}
            </tbody>
            </table>
        );
    }

    getRegionSelect() {
        return (
            <select id="region" name="Region Select" onChange={this.onSelectRegion} placeholder='Select your region'>
                <option value="NA">NA</option>
                <option value="EUW">EUW</option>
                <option value="EUNE">EUNE</option>
                <option value="KR">KR</option>
                <option value="LAN">LAN</option>
            </select>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderPage(this.state.forecasts);

        return (
            <div>
            <h1 id="tabelLabel" >League Match History</h1>
            <p>This page demonstrates fetching data from the Riot Games APIs for a player's recent matches data.</p>
                {contents}
            </div>
        );
    }

    async populateWeatherData() {
        const response = await fetch('weatherforecast');
        const data = await response.json();
        this.setState({ forecasts: data, loading: false });
    }
}
