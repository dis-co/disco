import * as React from "react"
import * as $ from "jquery"
import ContentEditable from "react-contenteditable"
import { xand } from "../Util"

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;
const DECIMAL_DIGITS = 2;

type UpdateFn = (index: number, value: any) => void

function startDragging(posY: number, index: number, value: number, update: UpdateFn) {
    console.log("Input drag start", index, posY)
    $(document)
        .on("contextmenu.drag", e => {
            e.preventDefault();
        })
        .on("mousemove.drag", e => {
            var diff = posY - e.clientY;
            console.log("Input drag mouse Y diff: ", diff);
            value += diff;
            posY = e.clientY;
            if (diff !== 0)
                update(index, value);
        })
        .on("mouseup.drag", e => {
            console.log("Input drag stop", e.clientY)
            $(document).off("mousemove.drag mouseup.drag contextmenu.drag");
        })
}

export function formatValue(value: any) {
    return typeof value === "number" ? value.toFixed(DECIMAL_DIGITS) : String(value);
}

export function addInputView(index: number, value: any, useRightClick: boolean, update: UpdateFn) {

    let tagName = "span", // TODO: Pass tag name as parameter
        typeofValue = typeof value,
        props = { key: index } as any,
        formattedValue = formatValue(value);

    // Boolean values, not editable
    if (typeofValue === "boolean") {
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

        return React.createElement(tagName, props, formattedValue);
    }

    // Numeric values, draggable
    if (typeofValue === "number") {
        props.onMouseDown = (ev: React.MouseEvent<HTMLElement>) => {
            if (xand(ev.button === RIGHT_BUTTON, useRightClick))
                startDragging(ev.clientY, index, value, update);
        }
        if (useRightClick) {
            props.onContextMenu = (ev: React.MouseEvent<HTMLElement>) => {
                ev.preventDefault();
            }
        }
    }

    let view;
    return <ContentEditable
            ref={el => el != null ? view = el : null}
            tagName={tagName}
            html={formattedValue}
            disabled={false}
            onKeyDown={ev => {
                if (ev.which === ENTER_KEY) {
                    ev.preventDefault();
                    update(index, ev.target.textContent);
                    ev.target.blur();
                }
                else if (ev.which === ESCAPE_KEY && view != null) {
                    ev.preventDefault();
                    ev.target.blur();
                    // TODO: Best way to revert change
                    update(index, formattedValue);
                }
            }}
            onBlur={ev => {
                update(index, ev.target.textContent);
            }}
            {...props}
            />
}