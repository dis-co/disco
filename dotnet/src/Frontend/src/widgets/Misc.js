import React, { Component } from 'react';
import { PlayButton, Button, Input, AutoCall, SpanEditable, Atom, More, Less, Output} from "./Buttons";
import { List, ListSorted, Row } from "./Boxes";

// Default values
const COLUMN_OFFSET = 5;

function ensureArray(ar) {
    return Array.isArray(ar) ? ar : [ar];
}

function unit(value, defaultValue, defaultUnit) {
    defaultUnit = defaultUnit || "";
    value = value != null ? value : defaultValue;
    return typeof value === "number" ? value.toString() + defaultUnit : value;
}

function rangeTo(length, f) {
    var ar = new Array(length);
    for (let i = 0; i < length; i++) {
        ar[i] = f(i);
    }
    return ar;
}

export const PlayButton = props =>
    <button className="icon icon-play">{props.children}</button>

export const Button = props =>
     <button>{props.children}</button>

// export const Input = props =>
//        <input type="text" placeholder={props.children} />

export const Input = props =>
    <span className="contentEditable" contentEditable="true">{props.children}</span>


export const AutoCall = props =>
    <button className="icon icon-autocall">{props.children}</button>

export const Atom = props =>
    <div className="atom"><span>{props.children}</span>
        <span>1200</span>
    </div>

export const More = props =>
    <button className="icon icon-control icon-more">{props.children}</button>

export const Less = props =>
    <button className="icon icon-control icon-less">{props.children}</button>

export const Output = props =>
    <span className="iris-output icon icon-host"> {props.children} <span className="icon icon-bull iris-status-off"></span></span>

const widthProps = (props) => ({
    marginLeft: unit(props.offset, COLUMN_OFFSET, "px"),
    width: unit(props.width, "inherit", "px"),
    flex: unit(props.flex),
})


const renderColumn = (child, index) =>
    <div key={index}
        style={widthProps(child.props)}>
        {child}
    </div>

function renderColumns(props) {
    var children = ensureArray(props.children);
    return rangeTo(children.length, i =>
        renderColumn(children[i], i)
    );
}

function renderLabels(props) {
    var children = ensureArray(props.children);
    const hasLabels = children.some(x => x.props.label);
    if (!hasLabels) {
        return null;
    }
    return (
      <li key="labels" className="iris-list-label-row">
        {rangeTo(children.length, i => {
            const child = children[i];
            return (<div className="iris-list-label" key={i} style={widthProps(child.props)}>
                {child.props.label}
            </div>)})}
        </li>
    )
}

export const List = props =>
    <ul className="iris-list">
        {renderLabels(props)}
        {rangeTo(props.rows, row =>
            <li key={row}>{renderColumns(props)}

            </li>)}

    </ul>

export const Row = props => {
    var children = ensureArray(props.children).slice();
    if (typeof props.label === "string") {
        children.splice(0, 0, <div className="iris-row-label">{props.label}</div>);
    }
    return <li>{children}</li>
}

// Pattern List
export const ListSorted = props =>
     <ul className="iris-listSorted">
         {props.children}
     </ul>

export class TitleBar extends Component {
  constructor(props) {
    super(props);
  }
  render() {
    return <div>{this.props.children}</div>
  }
}

export class Widget extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    var children = ensureArray(this.props.children).slice();
    var titleBar = children.filter(x => x != null && x.type === TitleBar);
    children = children.filter(x => x != null && x.type !== TitleBar);

    return (
      <div className="iris-widget" style={{
          width: this.props.width,
          height: this.props.height,
          border: "1px solid grey"
        }}>
        <div className="iris-draggable-handle">
          <span>{this.props.title}</span>
          {titleBar.length > 0 ? titleBar[0] : null}
          <div className="WidgetWindowControl">
          <button className="iris-icon icon-control icon-resize" onClick={() => {
            {/*this.props.global.addTab(this.props.model, this.props.id);
            this.props.global.removeWidget(this.props.id);*/}
          }}></button>
          <button className="iris-icon icon-control icon-close" onClick={() => {
            {/*this.props.global.removeWidget(this.props.id);*/}
          }}></button>
          </div>
        </div>
        {children}
      </div>
    )
  }
}

export const CueEditor = props =>
  <Widget title="Cue Editor">
      <TitleBar>
          <PlayButton>GO</PlayButton>
          <Button>GO</Button>
      </TitleBar>
      <List rows={10}>
        <More width={15}></More>
        <PlayButton width={15}></PlayButton>
        <Input label="Nr.:" width={40} offset={10}>0000</Input>
        <Input label="Cue Name" width={140}>Untitled</Input>
        <Input label="Delay" width={50} offset={20}>00:00:00</Input>
        <Input label="Shortkey" width={50} offset={20}>shortkey</Input>
        <AutoCall label="AutoCall" width={40}></AutoCall>
      </List>
      <ListSorted>
        <Row label ="VVVV/design.4vp"><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom></Row>
        <Row label ="VVVV/design.4vp"><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom></Row>
      </ListSorted>
      {props.children}
  </Widget>

  export const Design = props =>
  <Widget title="hohohohohoo">
    <TitleBar>
          <PlayButton>GO</PlayButton>
          <Button>GO</Button>
      </TitleBar>
      <ListSorted>
        <Row label ="VVVV/design.4vp"><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom></Row>
        <Row label ="VVVV/design.4vp"><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom><Atom>hoho</Atom></Row>
      </ListSorted>
      {props.children}
  </Widget>

  export const Cluster = props =>
  <Widget title="Hosts">
    <TitleBar>
          <PlayButton>GO</PlayButton>
          <Button>GO</Button>
      </TitleBar>
      <List rows={10}>
        <Output label="Hosts" width={80}>Wilhelm</Output>
        <Input label ="IP" width={40} offset={10}>0000</Input>
        <Input width={140}>Untitled</Input>
        <Input width={50} offset={20}>00:00:00</Input>
        <Input width={50} offset={20}>shortkey</Input>
        <AutoCall width={40}></AutoCall>
      </List>
  </Widget>



