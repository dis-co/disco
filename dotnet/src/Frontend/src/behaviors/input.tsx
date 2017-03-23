import * as React from "react"
import * as $ from "jquery"

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;

interface InputState {
    editIndex: number,
    editText: string
}

type GenerateFn = (props: {}) => JSX.Element
type UpdateFn = (index: number, value: any) => void

function startDragging(index: number, value: number, update: UpdateFn) {
    console.log("drag start")
    $(document)
        .on("mousemove.drag", e => {
            value += e.offsetY;
            update(index, value);
        })
        .on("mouseup.drag", e => {
            console.log("drag stop")
            $(document).off("mousemove.drag mouseup.drag");
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
        index: number, value: any,
        parent: React.Component<{},InputState>,
        update: UpdateFn, generate: GenerateFn) {
    if (parent.state.editIndex === index) {
        return (<input
            key={index}
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
        let props = { key: index };
        switch (typeof value) {
            case "number":
                Object.assign(props, {
                    onDoubleClick: () => parent.setState({ editIndex: index, editText: String(value) }),
                    onMouseDown: (ev: React.MouseEvent<HTMLElement>) => {
                        // if (ev.button === RIGHT_BUTTON)
                            startDragging(index, value, update);
                    }
                })
                break;
            case "boolean":
                Object.assign(props, {
                    onContextMenu: (ev: React.MouseEvent<HTMLElement>) => {
                        ev.preventDefault();
                        update(index, !value);
                    }
                })
                break;
            case "string":
            default:
                Object.assign(props, {
                    onDoubleClick: () => parent.setState({ editIndex: index, editText: String(value) })
                })
                break;
        }

        return generate(props);
    }
}