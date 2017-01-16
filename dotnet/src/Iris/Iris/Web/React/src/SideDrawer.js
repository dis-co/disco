import * as React from "react";
import { showModal } from './App';
import CREATE_PROJECT from "./modals/CreateProject";
import { loadProject, listProjects } from "iris";

export default class SideDrawer extends React.Component {

  listProjects() {
    listProjects().then(projects => this.setState({ projects }));
  }

  constructor(props) {
    super(props);
    this.state = { projects: [] };
  }

  componentDidMount() {
    this.listProjects();

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
            <li><a onClick={() => showModal(CREATE_PROJECT, () => this.listProjects())}>Create</a></li>
            <li>
              <p>Load</p>
              <ul>
                {this.state.projects.map(name =>
                  <li key={name}><a onClick={() => loadProject(this.props.info, name)}>{name}</a></li>
                )}
              </ul>
            </li>
          </ul>
        </li>
      </ul>
    )
  }
}