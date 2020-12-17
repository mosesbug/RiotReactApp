import React, { Component } from "react";
import { PieChart } from 'react-minimal-pie-chart';
import './NavMenu.css';


export class GameHistory extends Component {
    static displayName = GameHistory.name;

    /** Private fields */
    __initialRegion = "NA1";
    __submittedName = "";

    constructor(props) {
        super(props);
        this.state = {
            games: [], cardStats: [], loading: true, tableLoading: false,
            summonerName: "", region: ""
        };

        this.onSelectRegion = this.onSelectRegion.bind(this);
        this.onEntryFormSubmit = this.onEntryFormSubmit.bind(this);
        this.onChangeInput = this.onChangeInput.bind(this);
    }

    componentDidMount() {
        this.populatePage();
    }

    renderPage(state) {
        return (
            <div>
                {this.renderFormAndCardSection(state.cardStats)}
                {this.renderGamesTable(state)}
            </div>
        );
    }

    renderEntryForm() {
        return (
            <div className="submit-forms">
                <form onSubmit={this.onEntryFormSubmit}>
                    <input className="border-radius-left" placeholder="Summoner Name" value={this.state.summonerName} onChange={this.onChangeInput} name="SummonerName" minLength="3" maxLength="16" />
                    <button class="btn btn-primary sicon-search sicon-white border-radius-right" type="submit">Search</button>
                    {this.getRegionSelect()}
                </form>
            </div>
        );
    }

    // Profile icon + Summoner Name + Summoner Level
    renderSummonerInfoDisplay(cardStats) {
        return (
            <div className="image-medium left-inner-card">
                <p className="card-value-text"><img src={cardStats.profileIconLink} alt=""></img> {cardStats.summonerName}</p>
                <p className="card-light-text no-margin-bottom">Lvl</p>
                <strong className="card-value-text">{cardStats.summonerLevel}</strong>
            </div>
        );
    }

    // Winrate + KDA
    renderPerformanceStats(cardStats) {
        return (
            <div>
                <p className="win-loss-piechart">{this.renderWinLossChart(cardStats)}</p>
                <p className="card-light-text no-margin-bottom">KDA</p>
                <strong className="card-value-text">{cardStats.kda}</strong>
            </div>
        );
    }

    renderWinLossChart(cardStats) {
        return (
            <PieChart
                data={this.getPieChartData(cardStats.winrate)}
                label={({ dataEntry }) => dataEntry.key}
                labelPosition={this.getLabelPosition(cardStats.winrate)}
            />
        );
    }

    getPieChartData(winrate) {
        return winrate === 100 ?
            [
                { title: winrate + '%', value: winrate, color: 'lightblue', key: 'W' },
            ] :
            [
                { title: winrate + '%', value: winrate, color: 'lightblue', key: 'W' },
                { title: (100 - winrate) + '%', value: 100 - winrate, color: 'lightpink', key: 'L' }
            ];
    }

    getLabelPosition(winrate) {
        return winrate === 100 ?
            0 : 50;
    }

    renderPlayerRating(cardStats) {
        return (
            <div className="rating">
                <p className="card-light-text">Rating</p>
                <strong className="rating-text card-value-text">{cardStats.rating}</strong>
            </div>
        );
    }

    renderSummonerCardInner(cardStats) {
        return (
            <div className="summoner-card inner-card">
                {this.renderSummonerInfoDisplay(cardStats)}
                {this.renderPerformanceStats(cardStats)}
                {this.renderPlayerRating(cardStats)}
            </div>
        );
    }

    renderSummonerCard(cardStats) {
        let summonerCardInner = cardStats && cardStats.summonerName ? this.renderSummonerCardInner(cardStats) :
            <div></div>;

        return (
            <div>
                {summonerCardInner}
            </div>
        );
    }

    renderFormAndCardSection(cardStats) {
        return (
            <div className="upper-section">
                { this.renderEntryForm() }
                { this.renderSummonerCard(cardStats)}
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
        const summName = this.getSummonerName();
        if (summName !== "") {
            this.__submittedName = summName;
            this.populateData();
        }
        event.preventDefault();
    }

    getSummonerName() {
        return this.state.summonerName;
    }

    getRegion() {
        return this.state.region.length > 0 ? this.state.region : this.__initialRegion;
    }

    renderGamesTable(state) {
        const games = state.games;
        let tableTitle = (games.length && games.length > 0) ?
            state.cardStats.summonerName + "'s last " + games.length + " games" : "";

        return (
            <table className="table" aria-labelledby="tabelLabel">
                <caption>{tableTitle}</caption>
                <thead>
                    <tr>
                    <th>Date</th>
                    <th>Win/Loss</th>
                    <th>Champion</th>
                    <th>KDA</th>
                    <th>Game Length</th>
                    <th>Game Mode</th>
                    </tr>
                </thead>
                { this.renderGameTableBodyOuter(state.games) }
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
                        <td>{game.kills} / {game.deaths} / {game.assists}</td>
                        <td>{game.gameLength}m</td>
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
            <select className="region-select border-radius-point-five" id="region" name="Region Select" value={ this.state.region } onChange={this.onSelectRegion} placeholder="Select your region">
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
            : this.renderPage(this.state);

        return (
            <div>
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

        const data = await response.json();

        if (data.statusCode !== 200) {

            this.handleBadRequest(data);
            this.setState({ games: [], cardStats: [], tableLoading: false });
        }
        else {
            this.setState({ games: data.games, cardStats: data.cardStats, tableLoading: false });
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
