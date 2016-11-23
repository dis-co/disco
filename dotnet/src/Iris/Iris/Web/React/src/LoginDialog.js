import React from 'react';
import Dialog from 'material-ui/Dialog';
import FlatButton from 'material-ui/FlatButton';
import RaisedButton from 'material-ui/RaisedButton';
import TextField from 'material-ui/TextField';

export default class LoginDialog extends React.Component {
  constructor(props) {
      super(props);
      this.state = { open: true, username: "", password: "" };
  }

  handleSubmit() {
    this.props.login(this.state.username, this.state.password);
    this.setState({status: 'waiting'});
  };

  componentWillReceiveProps(nextProps) {
    try {
      let status = nextProps.session.Status.StatusType.ToString();
      switch (status) {
        case "Authorized":
          this.setState({ open: false })
          break;
        case "Unauthorized":
          if (this.state.status === "waiting") {
            this.setState({ status: "rejected" })
          }
          break;
      }
    }
    catch (err) {
      console.log(err);
    }
  }

  getMessage() {
    switch (this.state.status) {
      case "waiting":
        return <p>Waiting for response</p>;
      case "rejected":
        return <p>Input data is not correct</p>;
    }
  }

  render() {
    const actions = [
    //   <FlatButton
    //     label="Cancel"
    //     primary={true}
    //     onTouchTap={this.handleClose.bind(this)}
    //   />,
      <FlatButton
        label="Submit"
        primary={true}
        disabled={!this.state.username || !this.state.password || this.state.status === "waiting"}
        onTouchTap={this.handleSubmit.bind(this)}
      />,
    ];

    return (
    <Dialog
        title="Login"
        actions={actions}
        modal={true}
        open={this.state.open}>
        <TextField
          floatingLabelText="Username"
          errorText={this.state.username ? "" : "This field is required"}
          onChange={ev => this.setState({username: ev.target.value})}
        /><br />
        <TextField
          type="password"
          floatingLabelText="Password"
          errorText="This field is required"
          onChange={ev => this.setState({password: ev.target.value})}
        /><br />
        {this.getMessage()}
    </Dialog>
    );
  }
}