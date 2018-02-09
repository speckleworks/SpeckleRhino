<template>
  <v-card class='receiver-content'>
    <!-- header - menu and title -->
    <v-layout align-center>
      <!-- speed dial menu -->
      <v-flex xs2 text-xs-center>
        <v-speed-dial v-model='fab' direction='right' left style='left:0' class='pa-0 ma-0'>
          <v-btn fab small :flat='paused' class='ma-0 teal elevation-0' slot='activator' v-model='fab' :loading='client.isLoading' :dark='!paused'>
            <v-icon>
              arrow_downward
            </v-icon>
            <v-icon>close</v-icon>
          </v-btn>
          <v-tooltip bottom>
            Bake the geometry into Rhino.
            <v-btn fab small class='yellow darken-3' @click.native='bakeClient' slot='activator'>
              <v-icon>play_for_work</v-icon>
            </v-btn>
          </v-tooltip>
          <v-btn fab small @click.native='togglePause' class='ma-1 black' dark>
            <v-icon>{{ paused ? "pause" : "play_arrow" }}</v-icon>
          </v-btn>
          <v-btn fab small class='red ma-1' @click.native='confirmDelete=true'>
            <v-icon>delete</v-icon>
          </v-btn>
        </v-speed-dial>
      </v-flex>
      <!-- title -->
      <v-flex xs10>
        <v-card-title primary-title class='pb-0 pl-1 pt-3' :class='{ faded: fab }' style='position: relative; transition: all .3s ease; left: 5px;'>
          <p class='headline mb-1'>
            {{ client.stream.name }}
          </p>
          <br>
          <div class='caption' style='display: block; width:100%'> <span class='grey--text text--darkenx'><code class='grey darken-2 white--text' style='user-select: all; cursor: pointer;'>{{ client.stream.streamId }}</code> {{paused ? "(paused)" : ""}} updated:
              <timeago :auto-update='10' :since='client.lastUpdate'></timeago></span>
          </div>
        </v-card-title>
      </v-flex>
    </v-layout>
    <!-- misc -->
    <v-progress-linear height='1' :indeterminate='true' v-if='client.isLoading'>
    </v-progress-linear>
    <v-alert color='info' v-model='client.expired' class='pb-0 pt-0 mt-3'>
      <v-layout>
        <v-flex class='text-xs-center'>Stream is outdated.
          <v-tooltip left>
            Force refresh.
            <v-btn dark small fab flat @click.native='refreshStream' slot='activator' class='ma-0 '>
              <v-icon>refresh</v-icon>
            </v-btn>
          </v-tooltip>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- card actions -->
    <v-slide-y-transition>
      <v-card-actions v-show='true' class='pl-2' :class='{ faded: fab }' style='transition: all .3s ease'>
        <v-spacer></v-spacer>
        <v-btn class='xs-actions' icon @click.native='toggleLayers' small>
          <v-icon class='xs-actions'>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
        </v-btn>
        <!--         <v-btn class='xs-actions' icon @click.native='toggleLog' small>
          <v-icon class='xs-actions'>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
        </v-btn> -->
        <!-- <v-btn class='xs-actions' icon @click.native='toggleChildren' small>
          <v-icon class='xs-actions'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
        </v-btn> -->
        <extra-view-menu :streamId='client.stream.streamId' :restApi='client.BaseUrl'></extra-view-menu>
      </v-card-actions>
    </v-slide-y-transition>
    <!-- layers -->
    <v-slide-y-transition>
      <v-card-text v-show='showLayers' class='pa-0'>
        <receiver-layers :layers='client.stream.layers' :clientId='client.ClientId'></receiver-layers>
      </v-card-text>
    </v-slide-y-transition>
    <!-- log -->
    <v-slide-y-transition>
      <v-card-text v-show='showLog' class='pa-0'>
        <!-- <blockquote class='section-title'>Log</blockquote> -->
        <div class='caption pa-2'>Client id: <code>{{client.ClientId}}</code></div>
        <div class='log pa-2'>
          <template v-for='log in client.log'>
            <div class='caption' mb-5>
              <v-divider></v-divider>
              {{ log.timestamp }}: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
      </v-card-text>
    </v-slide-y-transition>
    <!-- history -->
    <v-slide-y-transition>
      <v-card-text v-show='showChildren' xxxclass='grey darken-4'>
        <blockquote class='section-title'>Children:</blockquote>
        <!-- <br> {{ client.stream.children.length == 0 ? 'Stream has no children.' : '' }} -->
        <template v-for='kid in client.stream.children'>
          <v-btn small block>
            {{kid}}
          </v-btn>
        </template>
        <br>
        <blockquote class='section-title'>Parent:</blockquote>
        <br> {{ client.stream.parent ? client.stream.parent : 'Stream is a root element.'}}
        <v-btn v-if='client.stream.parent' small block> {{client.stream.parent}} </v-btn>
        <br>
      </v-card-text>
    </v-slide-y-transition>
    <!-- confirm delete dialog -->
    <v-dialog v-model='confirmDelete'>
      <v-card>
        <v-card-title class='headline'>Are you sure you want to delete this receiver?</v-card-title>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native='confirmDelete=false'>Cancel</v-btn>
          <v-btn color='red' class='' @click.native='removeClient'>Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>
<script>
import ReceiverLayers from './ReceiverLayers.vue'
import ExtraViewMenu from './ExtraViewMenu.vue'

export default {
  name: 'Receiver',
  components: {
    ReceiverLayers,
    ExtraViewMenu
  },
  props: {
    client: Object
  },
  computed: {},
  data( ) {
    return {
      fab: false,
      enableRefresh: false,
      showLayers: false,
      showLog: false,
      showChildren: false,
      visible: true,
      paused: false,
      showMenu: false,
      fadeMenu: false,
      confirmDelete: false
    }
  },
  methods: {
    togglePause( ) {
      this.paused = !this.paused
      Interop.setClientPause( this.client.ClientId, this.paused )
    },
    toggleVisibility( ) {
      this.visible = !this.visible
      Interop.setClientVisibility( this.client.ClientId, this.visible )
    },
    bakeClient( ) {
      Interop.bakeClient( this.client.ClientId )
    },
    toggleLog( ) {
      if ( this.showLog ) return this.showLog = false
      this.showLog = true
      this.showLayers = false
      this.showChildren = false
    },
    toggleLayers( ) {
      if ( this.showLayers ) return this.showLayers = false
      this.showLayers = true
      this.showLog = false
      this.showChildren = false
    },
    toggleChildren( ) {
      if ( this.showChildren ) return this.showChildren = false
      this.showLayers = false
      this.showLog = false
      this.showChildren = true
    },
    removeClient( ) {
      this.confirmDelete = false
      this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
    },
    refreshStream( ) {
      Interop.refreshClient( this.client.ClientId )
    }
  },
  mounted( ) {
    // does nothing
  }
}
</script>
<style lang='scss'>
.faded {
  opacity: 0.2
}

.section-title {
  padding: 2px 0 2px 24px;
}

.log {
  max-height: 210px;
  overflow: auto;
}
</style>