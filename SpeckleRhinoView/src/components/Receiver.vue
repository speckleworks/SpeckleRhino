<template>
  <v-card class='receiver-content'>
    <v-divider></v-divider>
    <v-card-title primary-title>
      <div>
        <span class='headline'>
          {{ client.stream.name }}
        </span>
        <br>
        <span class='grey--text'><code>{{ client.stream.streamId }}</code> (receiver) </span>
        <div class='caption'>Last updated at:  <timeago :auto-update='10' :since='client.lastUpdate'></timeago></div>
      </div>
    </v-card-title>
    <v-progress-linear height='3' :indeterminate='true' v-if='client.isLoading'></v-progress-linear>
    <v-alert color='info' v-model='client.expired' >
      <v-layout align-center>
        <v-flex>There are updates available.</v-flex>
        <v-flex><v-btn dark small fab @click.native='refreshStream'><v-icon>refresh</v-icon></v-btn></v-flex>
      </v-layout>
    </v-alert>
    <v-card-actions>
      <v-btn icon @click.native='toggleLayers' fab small>
        <v-icon>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <v-btn icon @click.native='toggleLog' fab small>
        <v-icon>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
      </v-btn>
      <v-btn icon @click.native='toggleChildren' fab small>
        <v-icon>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
      </v-btn>
      <v-spacer></v-spacer>
      <v-btn icon flat color='yellow xxxlighten-3' small @click.native='bakeClient'>
        <v-icon>play_for_work</v-icon>
      </v-btn>
      <v-btn icon flat small @click.native='toggleVisibility'>
        <v-icon>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
      <v-btn icon flat color='light-blue xxxlighten-3' small @click.native='togglePause'>
        <v-icon>{{ paused ?  "pause_circle_outline" : "play_circle_outline" }}</v-icon>
      </v-btn>
      <v-btn icon flat color='red xxxlighten-3' small @click.native='removeClient'>
        <v-icon>close</v-icon>
      </v-btn>
    </v-card-actions>
    <v-slide-y-transition>
      <v-card-text v-show='showLayers' xxxclass='grey darken-4'>
        <blockquote class='section-title'>Layers:</blockquote>
        <receiver-layers :layers='client.stream.layers' :clientId='client.ClientId'></receiver-layers>
      </v-card-text>
    </v-slide-y-transition>
    <v-slide-y-transition>
      <v-card-text v-show='showLog' xxxclass='grey darken-4'>
        <blockquote class='section-title'>Log</blockquote>
        <br>
        <div class='log'>
          <template v-for='log in client.log'> 
            <div class='caption' mb-5>
            <v-divider></v-divider>
            {{ log.timestamp }}: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
        <div class='caption'>Client id: <code>{{client.ClientId}}</code></div>
      </v-card-text>
    </v-slide-y-transition>
    <v-slide-y-transition>
      <v-card-text v-show='showChildren' xxxclass='grey darken-4'>
        <blockquote class='section-title'>Children:</blockquote>
        <br>
        {{ client.stream.children.length == 0 ? 'Stream has no children.' : '' }}
        <template v-for='kid in client.stream.children'> 
          <v-btn small block>
            {{kid}}
          </v-btn>
        </template>
        <br>
        <blockquote class='section-title'>Parent:</blockquote>
        <br>
        {{ client.stream.parent ? client.stream.parent : 'Stream is a root element.'}}
        <v-btn v-if='client.stream.parent' small block> {{client.stream.parent}} </v-btn>
        <br>
      </v-card-text>
    </v-slide-y-transition>
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
    data() {
      return {
        enableRefresh: false,
        showLayers: false,
        showLog: false,
        showChildren: false,
        visible: true,
        paused: false
      }
    },
    methods: {
      togglePause() {
        this.paused = ! this.paused
        Interop.setClientPause( this.client.ClientId, this.paused )
      },
      toggleVisibility() {
        this.visible = !this.visible
        Interop.setClientVisibility( this.client.ClientId, this.visible )
      },
      bakeClient() {
        Interop.bakeClient( this.ClientId )
      },
      toggleLog() {
        if( this.showLog ) return this.showLog = false
        this.showLog = true
        this.showLayers = false
        this.showChildren = false
      },
      toggleLayers() {
        if( this.showLayers ) return this.showLayers = false
        this.showLayers = true
        this.showLog = false
        this.showChildren = false
      },
      toggleChildren() {
        if( this.showChildren ) return this.showChildren = false
        this.showLayers = false
        this.showLog = false
        this.showChildren = true
      },
      removeClient() {
        this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
      },
      refreshStream() {
        Interop.refreshClient( this.client.ClientId )
      }
    },
    mounted() {

    }
  }
</script>

<style lang='scss'>
.section-title{
  padding: 2px 0 2px 24px;
}
.receiver-content {
  transition: all .3s ease;
}
.receiver-content:hover{ 
  /*background-color: #E6E6E6;*/
}

.log {
  max-height: 210px;
  overflow: auto;
}
</style>