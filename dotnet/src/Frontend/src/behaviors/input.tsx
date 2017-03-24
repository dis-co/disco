import * as React from "react"
import * as $ from "jquery"
import { xand } from "../Util"

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;
const DECIMAL_DIGITS = 2;

interface InputState {
    editIndex: number,
    editText: string
}

type GenerateFn = (formattedValue: any, props: {}) => JSX.Element
type UpdateFn = (index: number, value: any) => void

function startDragging(posY: number, index: number, value: number, update: UpdateFn) {
    console.log("drag start", index, posY)
    $(document)
        .on("contextmenu.drag", e => {
            e.preventDefault();
        })
        .on("mousemove.drag", e => {
            var diff = posY - e.clientY;
            // console.log("Mouse Y diff: ", diff);
            value += diff;
            posY = e.clientY;
            if (diff !== 0)
                update(index, value);
        })
        .on("mouseup.drag", e => {
            console.log("drag stop", e.clientY)
            $(document).off("mousemove.drag mouseup.drag contextmenu.drag");
        })
}

function handleKeyDown(index: number, keyCode: number, value: string, update: UpdateFn) {
    if (keyCode === ESCAPE_KEY) {
        this.setState({editIndex: -1});
    }
    else if (keyCode === ENTER_KEY) {
        update(index, value);
        this.setState({editIndex: -1});
    }
}

export function addInputView(
        index: number, value: any, useRightClick: boolean,
        parent: React.Component<{},InputState>,
        update: UpdateFn, generate: GenerateFn) {
    if (parent.state.editIndex === index) {
        return (<input
            key={index}
            ref={el => el != null ? el.focus() : void 0}
            value={parent.state.editText}
            onBlur={ev => parent.setState({editIndex: -1})}
            onChange={ev => parent.setState({editText: ev.target.value})}
            onKeyDown={ev => {
                if (ev.which === ESCAPE_KEY) {
                    parent.setState({editIndex: -1});
                }
                else if (ev.which === ENTER_KEY) {
                    update(index, (ev.target as any).value);
                    parent.setState({editIndex: -1});
                }
            }}
          />)
    }
    else {
        let props: any = { key: index }, formattedValue = value;
        switch (typeof value) {
            case "number":
                formattedValue = value.toFixed(DECIMAL_DIGITS);
                props.onDoubleClick = () =>
                    parent.setState({ editIndex: index, editText: String(value) });
                props.onMouseDown = (ev: React.MouseEvent<HTMLElement>) => {
                    if (xand(ev.button === RIGHT_BUTTON, useRightClick))
                        startDragging(ev.clientY, index, value, update);
                }
                if (useRightClick) {
                    props.onContextMenu = (ev: React.MouseEvent<HTMLElement>) => {
                        ev.preventDefault();
                    }
                }
                break;
            case "boolean":
                formattedValue = value.toString();
                if (useRightClick) {
                    props.onContextMenu = (ev: React.MouseEvent<HTMLElement>) => {
                        ev.preventDefault();
                        update(index, !value);
                    }
                }
                else {
                    props.onClick = (ev: React.MouseEvent<HTMLElement>) => {
                        if (ev.button !== RIGHT_BUTTON)
                            update(index, !value);
                    }
                }
                break;
            case "string":
            default:
                formattedValue = String(value);
                Object.assign(props, {
                    onDoubleClick: () => parent.setState({ editIndex: index, editText: String(value) })
                })
                break;
        }

        return generate(formattedValue, props);
    }
}