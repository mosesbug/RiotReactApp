import React, { Component } from "react";
import './NavMenu.css';

export class GameHistory extends Component {
    static displayName = GameHistory.name;

    /** Private fields */
    __initialRegion = "NA1";
    __submittedName = "";

    constructor(props) {
        super(props);
        this.state = {
            games: [], loading: true, tableLoading: false,
            summonerName: "", region: ""
        };

        this.onSelectRegion = this.onSelectRegion.bind(this);
        this.onEntryFormSubmit = this.onEntryFormSubmit.bind(this);
        this.onChangeInput = this.onChangeInput.bind(this);
    }

    componentDidMount() {
        this.populatePage();
    }

    renderPage(games) {
        return (
            <div>
                { this.renderEntryForm() } 
                { this.renderGamesTable(games) }
            </div>
        );
    }

    renderEntryForm() {
        return (
            <div className="submit-forms">
                <form onSubmit={this.onEntryFormSubmit}>
                    <p>
                        <input type="text" placeholder="Summoner Name" value={this.state.summonerName} onChange={this.onChangeInput} name="SummonerName" minLength="3" maxLength="16" />
                        {this.getRegionSelect()}
                    </p>
                    <p><button>Get Match History</button></p>
                </form>
            </div>
        );
    }

    onSelectRegion(event) {
        // get the current region selection and store it in a property
        this.setState({ region: event.target.value });
        event.preventDefault();
    }

    onChangeInput(event) {
        this.setState({ summonerName: event.target.value });
        event.preventDefault();
    }

    onEntryFormSubmit(event) {
        // TODO: Make a debug mode
        // alert("Summoner name: " + this.getSummonerName() + "\nRegion: " + this.getRegion()); // MHB TURN OFF FOR NOW
        this.__submittedName = this.getSummonerName();
        this.populateData();
        event.preventDefault();
    }

    getSummonerName() {
        return this.state.summonerName;
    }

    getRegion() {
        return this.state.region.length > 0 ? this.state.region : this.__initialRegion;
    }

    renderGamesTable(games) {
        let tableTitle = (games.length && games.length > 0) ?
            this.__submittedName + "'s last 10 games" : "";

        return (
            <table className="table" aria-labelledby="tabelLabel">
                <caption>{tableTitle}</caption>
                <thead>
                    <tr>
                    <th>Date</th>
                    <th>Win/Loss</th>
                    <th>Champion</th>
                    <th>Game Length (minutes)</th>
                    <th>Game Mode</th>
                    </tr>
                </thead>
                { this.renderGameTableBodyOuter(games) }
            </table>
        );
    }

    renderGameTableBodyOuter(games) {
        let gameTable;
        if (this.state.tableLoading) {
            gameTable = <div className="placeholder-text">Loading...</div>;
        }
        else if (games.length && games.length > 0) {
            gameTable = this.renderGamesTableBodyInner(games);
        }
        else {
            gameTable = <div className="placeholder-text">Please enter valid search criteria</div>;
        }

        return gameTable;
    }

    renderGamesTableBodyInner(games) {
        return (
            <tbody>
                {games.map(game =>
                    <tr className={ this.getTableRowStyle(game)} key={game.date}>
                        <td>{game.date}</td>
                        <td>{game.result}</td>
                        <td>{this.renderChampionDisplay(game)}</td>
                        <td>{game.gameLength}</td>
                        <td>{game.queueType}</td>
                    </tr>
                )}
            </tbody>
        )
    }

    renderChampionDisplay(game) {
        return (
            <div className="image-small">
                <img src={game.championImage} title={game.championName} alt=""></img>
            </div>
        );
    }

    getTableRowStyle(game) {
        return game.result === "Win" ?
            "win-row" : "loss-row";
    }

    getRegionSelect() {
        return (
            <select id="region" name="Region Select" value={ this.state.region } onChange={this.onSelectRegion} placeholder="Select your region">
                <option value="NA1">NA</option>
                <option value="EUW1">EUW</option>
                <option value="EUN1">EUNE</option>
                <option value="KR">KR</option>
            </select>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderPage(this.state.games);

        return (
            <div>
            <h1 id="tabelLabel" >League Match History</h1>
            <p>This page demonstrates fetching data from the Riot Games APIs for a player's recent matches data.</p>
                {contents}
            </div>
        );
    }

    async populateData() {
        this.setState({ games: [], tableLoading: true });
        const response = await fetch("game", {
            method: 'post',
            body: JSON.stringify({
                SummonerName: this.getSummonerName(),
                Region: this.getRegion()
            })
        });

        // TODO: Add a summoner info card on the left side of the table?
        // Some cool info to include:
            // 1) Profile icon
            // 2) Summoner level
            // 3) Name
            // 4) Average KDA
            // 5) Favorite champ
        const data = await response.json();

        if (data.statusCode !== 200) {

            this.handleBadRequest(data);
            this.setState({ games: [], tableLoading: false });
        }
        else {
            this.setState({ games: data.games, tableLoading: false });
        }
    }

    // TODO: Turn into a nice popup or toast?
    handleBadRequest(gameResponse) {
        alert("Status code: " + gameResponse.statusCode + "\nMessage: " + gameResponse.errorMessage);
    }

    populatePage() {
        this.setState({ loading: false });
    }
}
