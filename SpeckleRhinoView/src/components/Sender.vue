<template>
  <v-card class='receiver-content' @mouseover='mouseOver' @mouseleave='mouseOut'>
    <!-- <v-divider></v-divider> -->
    <v-card-title primary-title class='pb-0 pt-3'>
      <div>
        <div class='headline mb-1'>
          <v-btn icon small class='teal ma-0'><v-icon class='grey--text text--lighten-3 make-me-small'>cloud_upload</v-icon></v-btn>
           {{ client.stream.name }}
        </div>
        <div class='caption'> <span class='grey--text'><code class='grey darken-2 white--text'>{{ client.stream.streamId }}</code></span> Last updated:
          <timeago :auto-update='10' :since='client.lastUpdate'></timeago>
        </div>
      </div>
    </v-card-title>
    <v-progress-linear height='3' :indeterminate='true' v-if='client.isLoading'></v-progress-linear>
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
    <v-slide-y-transition>
      <v-card-actions v-show='true' class='pl-2'>
        <v-btn icon @click.native='toggleLayers' small>
          <v-icon class='make-me-small'>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
        </v-btn>
        <v-btn icon @click.native='toggleLog' small>
          <v-icon class='make-me-small'>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
        </v-btn>
        <v-btn icon @click.native='toggleChildren' small>
          <v-icon class='make-me-small'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
        </v-btn>
        <v-spacer></v-spacer>
        <v-btn icon flat color='light-blue xxxlighten-3' small @click.native='togglePause'>
          <v-icon class='make-me-small'>{{ paused ? "pause_circle_outline" : "play_circle_outline" }}</v-icon>
        </v-btn>
        <v-btn icon flat color='red xxxlighten-3' small @click.native='removeClient'>
          <v-icon class='make-me-small'>close</v-icon>
        </v-btn>
      </v-card-actions>
    </v-slide-y-transition>
    <v-slide-y-transition>
      <v-card-text v-show='showLayers' class='pa-0'>
        <sender-layers :layers='client.stream.layers' :objects='client.stream.objects'></sender-layers>
      </v-card-text>
    </v-slide-y-transition>
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
  </v-card>
</template>
<script>
import SenderLayers from './SenderLayers.vue'

export default {
  name: 'Sender',
  props: {
    client: Object
  },
  components: {
    SenderLayers
  },
  computed: {},
  data( ) {
    return {
      showLayers: false,
      showLog: false,
      showChildren: false,
      showMenu: false,
      paused: false
    }
  },
  methods: {
    mouseOver() {
      this.showMenu = true
    },
    mouseOut() {
      this.showMenu = false
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
      this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.make-me-small {
  font-size: 15px !important;
}
</style>