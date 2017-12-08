// Adapted from ContentEditable.js ;)

import * as React from "react"

const ESCAPE_KEY = 27
const ENTER_KEY = 13

const createOpt = (name, id, selected) => {
  var opt = document.createElement('option')
  opt.innerHTML = name
  opt.value = id
  if (selected) opt.selected = "selected"
  return opt
}

export default class DropdownEditable extends React.Component {
  constructor(props) {
    super(props)
    this.state = { disabled: true }
  }

  render() {
    var { tagName, html, ...props } = this.props

    return React.createElement(
      tagName || 'div',
      {
        ...props,
        ref: (e) => this.htmlEl = e,
        // onInput: this.emitChange.bind(this),
        // onBlur: this.props.onBlur || this.emitChange.bind(this),
        onBlur: ev => {
          this.setState({disabled: true})
        },
        onKeyDown: ev => {
          if (!this.state.disabled) {
            if (ev.which === ENTER_KEY) {
                ev.preventDefault()
                this.setState({disabled: true})
                this.props.onChange(ev.target.innerHTML)
            }
            else if (ev.which === ESCAPE_KEY) {
                ev.preventDefault()
              this.setState({disabled: true})
            }
          }
        },
        onClick: ev => {
          if (this.state.disabled) {
            // Capture htmlEl as React may reuse the event
            var options = this.props["data-options"]
            var htmlEl = ev.target
            this.setState({disabled: false}, () => {
              let emptyStr = "--"
              let cueList = this.props["data-selected"]
              var select = document.createElement('select')
              select.classList.add("disco-control")
              select.classList.add("disco-select")

              select.appendChild(createOpt(emptyStr, null, cueList === null))
              options.forEach(([ name, id ]) => {
                select.appendChild(createOpt(name, id, cueList === id))
              })

              htmlEl.innerHTML = ""
              htmlEl.appendChild(select)
              select.onchange = el => {
                if(el.target.value === emptyStr) {
                  this.props.onChange(null)
                } else {
                  this.props.onChange(el.target.value)
                }
                this.setState({ disabled: true })
              }
              select.focus()
            })
          }
        },
        contentEditable: !this.state.disabled,
        dangerouslySetInnerHTML: {__html: html}
      },
      this.props.children)
  }

  shouldComponentUpdate(nextProps, nextState) {
    // We need not rerender if the change of props simply reflects the user's
    // edits. Rerendering in this case would make the cursor/caret jump.
    return (
      // Rerender if there is no element yet... (somehow?)
      !this.htmlEl
      // ...or if html really changed... (programmatically, not by user edit)
      || ( nextProps.html !== this.htmlEl.innerHTML
        && nextProps.html !== this.props.html )
      // ...or if editing is enabled or disabled.
      || this.state.disabled !== nextState.disabled
    )
  }

  componentDidUpdate() {
    if ( this.htmlEl && this.props.html !== this.htmlEl.innerHTML ) {
      // Perhaps React (whose VDOM gets outdated because we often prevent
      // rerendering) did not update the DOM. So we update it manually now.
      this.htmlEl.innerHTML = this.props.html
    }
  }
}
