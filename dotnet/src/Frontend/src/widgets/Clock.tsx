import * as React from "react"
import { IDisposable, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"

declare var Iris: IIris;

export default class ClockView extends React.Component<{global: GlobalModel},{clock: number}> {
  disposables: IDisposable[];

  constructor(props) {
    super(props);
    this.state = { clock: 0 };
  }

  componentDidMount() {
    this.disposables = [];

    this.disposables.push(
      this.props.global.subscribe("clock", clock => this.setState({clock}))
    );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.dispose());
    }
  }  

  render() {
    return <span>Frames: {this.state.clock}</span>
  }
}
