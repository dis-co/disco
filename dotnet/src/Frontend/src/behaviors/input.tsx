import * as React from "react"
import * as $ from "jquery"
import ContentEditable from "../widgets/ContentEditable"
import { xand } from "../Util"

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;
const DECIMAL_DIGITS = 2;

type UpdateFn = (index: number, value: any) => void

function startDragging(posY: number, index: number, value: number, update: UpdateFn) {
    // console.log("Input drag start", index, posY)
    $(document)
        .on("contextmenu.drag", e => {
            e.preventDefault();
        })
        .on("mousemove.drag", e => {
            var diff = posY - e.clientY;
            // console.log("Input drag mouse Y diff: ", diff);
            value += diff;
            posY = e.clientY;
            if (diff !== 0)
                update(index, value);
        })
        .on("mouseup.drag", e => {
            // console.log("Input drag stop", e.clientY)
            $(document).off("mousemove.drag mouseup.drag contextmenu.drag");
        })
}

export function formatValue(value: any) {
    return typeof value === "number" ? value.toFixed(DECIMAL_DIGITS) : String(value);
}

export function addInputView(index: number, value: any, tagName, useRightClick: boolean, update: UpdateFn) {

    let typeofValue = typeof value,
        props = {} as any, //{ key: index } as any,
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

    return <ContentEditable tagName={tagName} html={formattedValue} onChange={html => update(index, html)} {...props} />
}