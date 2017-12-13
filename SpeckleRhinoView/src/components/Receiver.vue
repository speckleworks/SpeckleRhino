<template>
  <v-card class='receiver-content'>
    <!-- header - menu and title -->
    <v-layout>
      <!-- speed dial menu -->
      <v-flex class='xs2'>
        <v-speed-dial v-model='fab' direction='right' left style='top:15px' class='pa-0 ma-0'>
          <v-btn flat fab small class='ma-0 teal' slot='activator' v-model='fab'>
            <v-icon xxxclass='cyan--text xxxxs-actions'>
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
          <v-btn fab small @click.native='togglePause'>
            <v-icon>{{ paused ? "pause_circle_outline" : "play_circle_outline" }}</v-icon>
          </v-btn>
          <v-btn fab small class='red' @click.native='confirmDelete=true'>
            <v-icon>delete</v-icon>
          </v-btn>
        </v-speed-dial>
      </v-flex>
      <!-- title -->
      <v-flex>
        <v-card-title primary-title class='pb-0 pt-3' :class='{ faded: fab }' style='position: relative; transition: all .3s ease; left: 5px;'>
          <p class='headline mb-1'>
            {{ client.stream.name }}
          </p>
          <div class='caption'> <span class='grey--text text--darkenx'><code class='grey darken-2 white--text'>{{ client.stream.streamId }}</code> Last updated:
              <timeago :auto-update='10' :since='client.lastUpdate'></timeago></span>
          </div>
        </v-card-title>
      </v-flex>
    </v-layout>
    <!-- misc -->
    <v-progress-linear height='1' :indeterminate='true' v-if='client.isLoading'>
    </v-progress-linear>
    <v-alert color='info' v-model='client.expired'>
      <v-layout align-center>
        <v-flex>There are updates available.</v-flex>
        <v-flex>
          <v-btn dark small fab @click.native='refreshStream'>
            <v-icon>refresh</v-icon>
          </v-btn>
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
        <v-btn class='xs-actions' icon @click.native='toggleChildren' small>
          <v-icon class='xs-actions'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
        </v-btn>
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
        <br> {{ client.stream.children.length == 0 ? 'Stream has no children.' : '' }}
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

export default {
  name: 'Receiver',
  components: {
    ReceiverLayers
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

.receiver-content {
  transition: all .3s ease;
}

.log {
  max-height: 210px;
  overflow: auto;
}
</style>