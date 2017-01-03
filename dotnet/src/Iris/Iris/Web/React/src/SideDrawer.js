import * as React from "react";
import { showModal } from './App';
import LOAD_PROJECT from "./modals/LoadProject";

export default class SideDrawer extends React.Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
    // From: https://www.muicss.com/docs/v1/example-layouts/responsive-side-menu
    var $bodyEl = $('body'),
      $sidedrawerEl = $('#sidedrawer');

    function showSidedrawer() {
      // show overlay
      var options = {
        onclose: function() {
          $sidedrawerEl
            .removeClass('active')
            .appendTo(document.body);
        }
      };

      var $overlayEl = $(mui.overlay('on', options));

      // show element
      $sidedrawerEl.appendTo($overlayEl);
      setTimeout(function() {
        $sidedrawerEl.addClass('active');
      }, 20);
    }

    function hideSidedrawer() {
      $bodyEl.toggleClass('hide-sidedrawer');
    }

    $('.js-show-sidedrawer').on('click', showSidedrawer);
    $('.js-hide-sidedrawer').on('click', hideSidedrawer);

    // Hide/show categories on click
    var $titleEls = $('strong', $sidedrawerEl);
    $titleEls.next().hide();

    $titleEls.on('click', function() {
      $(this).next().slideToggle(200);
    });
  }

  render() {
    return (
      <ul>
        <li>
          <strong>Project</strong>
          <ul>
            <li><a href="#">Create</a></li>
            <li><a onClick={() => showModal(LOAD_PROJECT)}>Load</a></li>
          </ul>
        </li>
        <li>
          <strong>Category 2</strong>
          <ul>
            <li><a href="#">Item 1</a></li>
            <li><a href="#">Item 2</a></li>
            <li><a href="#">Item 3</a></li>
          </ul>
        </li>
        <li>
          <strong>Category 3</strong>
          <ul>
            <li><a href="#">Item 1</a></li>
            <li><a href="#">Item 2</a></li>
            <li><a href="#">Item 3</a></li>
          </ul>
        </li>
      </ul>
    )
  }
}