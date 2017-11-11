<template>
  <v-card>
    <v-divider></v-divider>
    <v-card-title primary-title>
      <div>
        <span class='headline'>
          <!-- <v-btn fab dark small flat color='cyan'>
            <v-icon>cloud_download</v-icon>
          </v-btn> -->
          {{ client.stream.name }}
        </span>
        <br>
        <span class='grey--text'>{{ client.stream.streamId }} (receiver) </span>
        <!-- <div class='grey--text text--lighten-2 caption'>Last updated at:</div> -->
      </div>
    </v-card-title>
    <v-card-actions>
      <v-btn icon @click.native='toggleLayers' fab small dark>
        <v-icon>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <v-btn icon @click.native='toggleLog' fab small dark>
        <v-icon>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
      </v-btn>
      <v-spacer></v-spacer>
      <v-btn icon flat small>
        <v-icon>play_for_work</v-icon>
      </v-btn>
      <v-btn icon flat small>
        <v-icon>visibility</v-icon>
      </v-btn>
      <v-btn icon flat color='light-blue' small>
        <v-icon>refresh</v-icon>
      </v-btn>
      <v-btn icon flat color='red lighten-1' small @click.native='removeClient'>
        <v-icon>close</v-icon>
      </v-btn>
    </v-card-actions>
    <v-slide-y-transition>
      <v-card-text v-show='showLayers' class='grey darken-4'>
        <blockquote>Layers:</blockquote>
        <v-list class='grey darken-4' two-line style='padding:0'>
          <v-list-tile v-for='layer in client.stream.layers' :key='layer.guid'>
            <v-list-tile-action>
              <v-btn icon>
                <v-icon color="grey lighten-1">visibility</v-icon>
              </v-btn>
            </v-list-tile-action>
            <v-list-tile-content>  
              <v-list-tile-title>{{ layer.name }}</v-list-tile-title>
              <v-list-tile-sub-title class="grey--text text--darken-1 caption">Objects: {{ layer.objectCount }}</v-list-tile-sub-title>
            </v-list-tile-content>
          </v-list-tile>
        </v-list>
      </v-card-text>
    </v-slide-y-transition>
    <v-slide-y-transition>
      <v-card-text v-show='showLog' class='grey darken-4'>
        <blockquote class=''>Log</blockquote>
        <br>
        <div class='log'>
          <template v-for='log in client.log'> 
            <div class='caption' mb-5>
            <v-divider></v-divider>
            <timeago :since='log.timestamp'></timeago>: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
        <div class='caption'>Client id: <code>{{client.ClientId}}</code></div>
      </v-card-text>
    </v-slide-y-transition>
  </v-card>
</template>

<script>

  export default {
    name: 'Receiver',
    props: {
      client: Object
    },
    components: {},
    computed: {},
    data() {
      return {
        enableRefresh: false,
        showLayers: false,
        showLog: false
      }
    },
    methods: {
      toggleLog() {
        if( this.showLog ) return this.showLog = false
        this.showLog = true
        this.showLayers = false
      },
      toggleLayers() {
        if( this.showLayers ) return this.showLayers = false
        this.showLayers = true
        this.showLog = false
      },
      removeClient() {
        this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
      }
    },
    mounted() {

    }
  }
</script>

<style lang='scss'>
.log {
  max-height: 210px;
  overflow: auto;
}
</style>